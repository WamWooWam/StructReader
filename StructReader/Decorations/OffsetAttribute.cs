using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class OffsetAttribute : Attribute
    {
        public OffsetAttribute(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }
}
