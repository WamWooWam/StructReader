using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class EndiannessAttribute : Attribute
    {
        public EndiannessAttribute(Endianness endianness)
        {
            Endianness = endianness;
        }

        public Endianness Endianness { get; }
    }
}
