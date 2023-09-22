using RoisLang.mid_ir;
using RoisLang.types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    public class StructLayout
    {
        // (size, align)
        Dictionary<ClassType, (int, int)> TypeInfo = new();
        Dictionary<ClassType, List<int>> FieldOffsets = new();

        public int GetFieldOffset(FieldInfo fi)
        {
            if (!FieldOffsets.ContainsKey(fi.Class))
                CalcOffsetsFor(fi.Class);
            return FieldOffsets[fi.Class][fi.FieldN];
        }

        public void CalculateReprFor(ClassType cls)
        {
            if (!FieldOffsets.ContainsKey(cls)) CalcOffsetsFor(cls);
        }

        private void CalcOffsetsFor(ClassType cls)
        {
            List<int> offsets = new();
            int offset = 0;
            int align = 0;
            foreach (var (_, field) in cls.Fields)
            {
                var repr = field.GetRepr();
                if (offset % repr.Align() != 0)
                    offset += repr.Align() - (offset % repr.Align());
                offsets.Add(offset);
                offset += repr.Size();
                if (repr.Align() > align) align = repr.Align();
            }
            FieldOffsets.Add(cls, offsets);
            if (offset % align != 0)
                offset += align - (offset % align);
            TypeInfo.Add(cls, (align, offset));
        }

        public void PrintRepresentations()
        {
            foreach (var (cls, fieldOffsets) in FieldOffsets)
            {
                var (align, size) = TypeInfo[cls];
                Console.WriteLine($"{cls} align:{align} size:{size}");
                for (int i = 0; i < fieldOffsets.Count; i++)
                    Console.WriteLine($"  {i} {cls.Fields[i].Item1}:{cls.Fields[i].Item2} at {fieldOffsets[i]}");
            }
        }
    }
}
