using System;

namespace WamWooWam.StructReader
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class EncodingAttribute : Attribute
    {
        public EncodingAttribute(string encoding)
        {
            Encoding = encoding;
        }

        public string Encoding { get; }
    }
}
