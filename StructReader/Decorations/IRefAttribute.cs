using System;
using System.Collections.Generic;
using System.Text;

namespace WamWooWam.StructReader
{
    public interface IRefAttribute<T>
    {
        string FieldName { get; }
        T GetRef(object obj);
    }
}
