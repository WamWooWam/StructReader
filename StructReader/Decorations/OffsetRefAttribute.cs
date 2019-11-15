using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class OffsetRefAttribute : Attribute, IRefAttribute<int>
    {
        /// <summary>
        /// Offset a field in with reference to another field.
        /// </summary>
        /// <param name="fieldName">The name of the field containing the offset</param>
        /// <param name="offset">An additional offset to add or subtract to the field's value. Set to 0 for an absolute offset.</param>
        public OffsetRefAttribute(string fieldName, int offset = 0)
        {
            FieldName = fieldName;
            Offset = offset;
        }

        public string FieldName { get; }
        public int Offset { get; }

        public int GetRef(object obj)
        {
            return Convert.ToInt32(obj.GetPropValue(FieldName)) + Offset;
        }
    }
}
