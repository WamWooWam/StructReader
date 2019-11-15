using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ArraySizeAttribute : Attribute
    {
        public ArraySizeAttribute(int size, ArraySizeType type = ArraySizeType.Elements)
        {
            Size = size;
            Type = type;
        }

        public int Size { get; }
        public ArraySizeType Type { get; }
    }
}
