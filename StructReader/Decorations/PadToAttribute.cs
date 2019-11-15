using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class PadToAttribute : Attribute
    {
        public PadToAttribute(uint padto)
        {
            PadTo = padto;
        }

        public uint PadTo { get; }
    }
}
