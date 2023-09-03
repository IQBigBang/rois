using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    /// <summary>
    /// The register allocator works, at the moment, at the block-level only.
    /// It uses a very simple "algorithm" of assigning registers until all are used
    /// and then failing (TODO: spilling to stack)
    /// </summary>
    public class RegAlloc
    {
        public Dictionary<MidValue, GpReg> RegAllocBlock(MidBlock block)
        {
            SortedSet<GpReg> allRegisters = new() { GpReg.Rcx, GpReg.Rdx, GpReg.R8, GpReg.R9, GpReg.Rdi, GpReg.Rsi, GpReg.Rax };
            SortedSet<GpReg> freeRegisters = new(allRegisters);
            Dictionary<MidValue, GpReg> allocations = new();
            List<KeyValuePair<MidValue, int>> liveRangeStarts = GetRegLiveRangeStarts(block);
            List<KeyValuePair<MidValue, int>> liveRangeEnds = GetRegLiveRangeEnds(block);
            
            // Now, onto the register allocation
            for (int i = -1; i < block.Instrs.Count; i++)
            {
                // first, end ranges = add registers back to the "free" list
                foreach (var endingReg in liveRangeEnds.Where(x => x.Value == i))
                {
                    // if `allocations` doesn't contain this register, it means the value is unused (it ends at the same instr it starts)
                    // use the special `RNull` psuedo-register for it
                    if (!allocations.ContainsKey(endingReg.Key))
                    {
                        liveRangeStarts.RemoveAll(x => x.Key == endingReg.Key);
                        allocations[endingReg.Key] = GpReg.RNull;
                    }
                    else
                    {
                        freeRegisters.Add(allocations[endingReg.Key]);
                    }
                }

                // if the instruction requires it, write LiveReg data
                if (i > -1 && block.Instrs[i] != null && block.Instrs[i].RequiresLiveRegData)
                {
                    block.Instrs[i].extra = new LiveRegData(allRegisters.Where(x => !freeRegisters.Contains(x)).ToList());
                }

                // then, assign new registers
                foreach (var startingReg in liveRangeStarts.Where(x => x.Value == i))
                {
                    var newReg = freeRegisters.Min;
                    freeRegisters.Remove(newReg);
                    allocations[startingReg.Key] = newReg;
                }
                // remove said registers from both lists
                liveRangeStarts.RemoveAll(x => x.Value == i);
                liveRangeEnds.RemoveAll(x => x.Value == i);
            }

            return allocations;
        }

        private List<KeyValuePair<MidValue, int>> GetRegLiveRangeStarts(MidBlock block)
        {
            Dictionary<MidValue, int> starts = new Dictionary<MidValue, int>();
            foreach (var arg in block.Arguments)
                starts[arg] = -1; // args start at minus one
            for (int i = 0; i < block.Instrs.Count; i++)
            {
                var instr = block.Instrs[i];
                if (instr == null || !instr.HasOut()) continue;
                starts[instr.GetOut()] = i;
            }
            return starts.ToList();
        }

        /// <summary>
        /// Calculates when registers become dead (the last time they're used)
        /// </summary>
        private List<KeyValuePair<MidValue, int>> GetRegLiveRangeEnds(MidBlock block)
        {
            Dictionary<MidValue, int> ends = new Dictionary<MidValue, int>();
            // fill with zeros
            foreach (var reg in block.AllRegisters())
                ends[reg] = -1;
            for (int i = 0; i < block.Instrs.Count; i++)
            {
                var instr = block.Instrs[i];
                if (instr == null) continue;
                foreach (var value in instr.AllArgs())
                {
                    if (!value.IsReg) continue;
                    ends[value] = i;
                }
            }
            return ends.ToList();
        }
    }

    public enum GpReg
    {
        Rcx = 0,
        Rdx,
        R8,
        R9,
        Rdi,
        Rsi,
        Rax = 6,
        RNull
    }
}
