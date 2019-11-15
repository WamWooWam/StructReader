using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ArraySizeRefAttribute : Attribute, IRefAttribute<uint>
    {
        public ArraySizeRefAttribute(string fieldName, ArraySizeType type = ArraySizeType.Elements)
        {
            FieldName = fieldName;
            Type = type;
        }

        public string FieldName { get; }
        public ArraySizeType Type { get; }

        public uint GetRef(object obj)
        {
            return Convert.ToUInt32(obj.GetPropValue(FieldName));
        }
    }
}
