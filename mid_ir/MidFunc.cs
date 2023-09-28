using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.mid_ir
{
    public class MidFunc
    {
        public readonly string Name;
        public readonly TypeRef FuncType;
        public readonly List<MidBlock> Blocks;
        public readonly bool IsExtern;
        public readonly ClassType? Self;

        public bool IsMethod => Self is not null;

        public MidBlock EntryBlock => Blocks[0];

        public MidFunc(string name, List<TypeRef> args, TypeRef ret, ClassType? self = null, bool isExtern = false)
        {
            Name = name;
            FuncType = types.FuncType.New(args, ret);
            var entryBlock = new MidBlock(0, args.ToList());
            Blocks = new List<MidBlock> { entryBlock };
            IsExtern = isExtern;
            Self = self;
        }

        public MidBlock NewBlock(List<TypeRef>? argumentTypes_ = null)
        {
            var block = new MidBlock((uint)Blocks.Count, argumentTypes_);
            Blocks.Add(block);
            return block;
        }

        public void Dump()
        {
            if (IsExtern)
            {
                Console.WriteLine($"extern def @{Name} : {FuncType}");
                return;
            }
            Console.WriteLine($"def @{Name} : {FuncType}:");
            Blocks.ForEach(block => block.Dump());
        }
    }
}
