using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace WamWooWam.StructReader
{
    public class DataReader
    {
        private ref struct ReaderContext
        {
            public int offset;
        }

        public static T Read<T>(byte[] data) where T : struct
        {
            var t = new T();
            Read(ref t, data);
            return t;
        }

        public static void Read<T>(ref T t, byte[] data) where T : struct
        {
            var context = new ReaderContext();
            Read(ref t, ref context, data);
        }

        private static void Read<T>(ref T t, ref ReaderContext context, byte[] data) where T : struct
        {

        }
    }
}
