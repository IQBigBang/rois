using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    /// <summary>
    /// The low-level representation of a type
    /// </summary>
    internal enum Repr
    {
        /// <summary>
        /// Zero - no runtime representation. Includes: void
        /// </summary>
        O,
        /// <summary>
        /// Value 32-bit. Includes: 32-bit integers, 32-bit floats
        /// </summary>
        V32,
        /// <summary>
        /// Value 64-bit. Includes: 64-bit integers, non-managed pointers
        /// </summary>
        V64,
        /// <summary>
        /// Address = managed (refcounted) pointer
        /// </summary>
        Addr,
    }

    internal static class ReprExtensions
    {
        public static int Size(this Repr repr) =>
            repr switch
            {
                Repr.O => 0,
                Repr.V32 => 4,
                Repr.V64 => 8,
                Repr.Addr => 8, // we assume x64 arch
            };

        public static int Align(this Repr repr) =>
            repr switch
            {
                Repr.O => 0,
                Repr.V32 => 4,
                Repr.V64 => 8,
                Repr.Addr => 8, // we assume x64 arch
            };

        public static Repr GetRepr(this types.TypeRef typ)
        {
            return typ switch
            {
                types.VoidType => Repr.O,
                types.BoolType or types.IntType => Repr.V32,
                types.FuncType => Repr.V64,
                types.ClassType => Repr.Addr,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
