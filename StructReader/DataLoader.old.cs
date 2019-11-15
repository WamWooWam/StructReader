using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WamWooWam.StructReader
{
    /// <summary>
    /// Hello this class needs a major rewrite and while I do plan to do that
    /// for now it's here in the mess it was in when I initially wrote it.
    /// </summary>
    public static class DataLoader
    {
        private static MethodInfo _loadMethod;
        private static Dictionary<Type, MethodInfo> _primitiveReaders = new Dictionary<Type, MethodInfo>();

        private static readonly Type[] _primitiveTypes = new[]
        {
            typeof(bool), typeof(byte), typeof(char), typeof(decimal),
            typeof(double), typeof(short), typeof(int), typeof(long),
            typeof(sbyte), typeof(float), typeof(ushort), typeof(uint),
            typeof(ulong)
        };

        static DataLoader()
        {
            _loadMethod = typeof(DataLoader).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(n => n.Name.StartsWith("Read"));
            var readPrimitive = typeof(DataLoader).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(n => n.Name.StartsWith("ReadPrimitive"));
            foreach (var item in _primitiveTypes)
            {
                _primitiveReaders.Add(item, readPrimitive.MakeGenericMethod(item));
            }
        }

        public static void Load<T>(ref T obj, Stream stream)
        {
            var array = new byte[stream.Length];
            var data = new Memory<byte>(array);
            stream.Read(array, 0, array.Length);
            int i = 0;

            Read(ref obj, ref i, null, data);
        }

        public static void Load<T>(ref T obj, byte[] array)
        {
            var data = new Memory<byte>(array);
            int i = 0;
            Read(ref obj, ref i, null, data);
        }

        private static void Read<T>(ref T obj, ref int offset, bool? bigEndian, ReadOnlyMemory<byte> data)
        {
            OffsetAttribute offsetAttribute;
            OffsetRefAttribute offsetRefAttribute;
            EndiannessAttribute endiannessAttribute;
            PadToAttribute padToAttribute;

            var boxed = (object)obj;
            var type = typeof(T);
            var fields = type.GetCachedFields().Values;
            var span = data.Span;

            if (bigEndian == null && (endiannessAttribute = type.GetCachedCustomAttribute<EndiannessAttribute>()) != null)
            {
                bigEndian = endiannessAttribute.Endianness == Endianness.Big;
            }

            var big_endian = bigEndian ?? false;

            foreach (var field in fields)
            {
                object value = boxed;

                if ((offsetAttribute = field.GetCachedCustomAttribute<OffsetAttribute>()) != null)
                {
                    offset = offsetAttribute.Offset;
                }

                if ((offsetRefAttribute = field.GetCachedCustomAttribute<OffsetRefAttribute>()) != null)
                {
                    offset = offsetRefAttribute.GetRef(value);
                }

                if (ReadValue(ref offset, ref data, ref span, big_endian, type, ref boxed, field.FieldType, field, ref value))
                {
                    field.SetValue(boxed, value);
                }
            }

            if ((padToAttribute = type.GetCachedCustomAttribute<PadToAttribute>()) != null)
            {
                offset += (int)(offset % padToAttribute.PadTo);
            }

            obj = (T)boxed;
        }

        private static unsafe bool ReadValue(ref int offset, ref ReadOnlyMemory<byte> data, ref ReadOnlySpan<byte> span,
                                             bool bigEndian, Type type, ref object boxed, Type valueType, FieldInfo info,
                                             ref object value)
        {
            FixedBufferAttribute fixedBufferAttribute;
            PadToAttribute padToAttribute;

            var offsetSpan = span.Slice((int)offset);
            try
            {
                if (valueType.IsPrimitive)
                {
                    var parameters = new object[] { offset, bigEndian, data.Slice(offset) };
                    value = _primitiveReaders[valueType]?.Invoke(null, parameters);
                    offset = (int)parameters[0];
                    return true;
                }
                else if ((fixedBufferAttribute = info.GetCachedCustomAttribute<FixedBufferAttribute>()) != null)
                {
                    ReadFixedArray(ref offset, ref data, ref span, bigEndian, type, ref boxed, info, ref value, fixedBufferAttribute);
                    return true;
                }
                else if (valueType.IsValueType)
                {
                    var method = _loadMethod.MakeGenericMethod(valueType);
                    var parameters = new object[] { Activator.CreateInstance(valueType), offset, bigEndian, data };
                    method.Invoke(null, parameters);

                    value = parameters[0];
                    offset = (int)parameters[1];

                    return true;
                }
                else if (valueType == typeof(string))
                {
                    ReadString(info, offsetSpan, ref offset, ref value);
                    return true;
                }
                else if (valueType.IsArray)
                {
                    ReadArray(ref offset, ref data, ref span, bigEndian, type, ref boxed, valueType, info, ref value);
                    return true;
                }
            }
            finally
            {
                if ((padToAttribute = info?.GetCachedCustomAttribute<PadToAttribute>()) != null)
                {
                    offset += (int)(offset % padToAttribute.PadTo);
                }
            }

            return false; // anything we can't read, we skip
        }

        private static void ReadString(FieldInfo info, ReadOnlySpan<byte> offsetSpan, ref int offset, ref object value)
        {
            EncodingAttribute encodingAttribute;
            var encoding = Encoding.UTF8;
            if ((encodingAttribute = info?.GetCachedCustomAttribute<EncodingAttribute>()) != null)
            {
                encoding = Encoding.GetEncoding(encodingAttribute.Encoding);
            }

            var nullChar = encoding.GetBytes("\0");
            var sb = new StringBuilder();
            var pos = 0;

            do
            {
                var chars = offsetSpan.Slice((int)pos, nullChar.Length);
                pos += nullChar.Length;

                if (chars.SequenceEqual(nullChar))
                {
                    break;
                }

                sb.Append(encoding.GetChars(chars.ToArray()));
            }
            while (pos < offsetSpan.Length);

            value = sb.ToString(); // trim null chars
            offset += pos;
        }

        private static unsafe T ReadPrimitive<T>(ref int offset, bool bigEndian, ReadOnlyMemory<byte> offsetSpan) 
        {
            var size = Unsafe.SizeOf<T>();

            fixed (byte* src = &MemoryMarshal.GetReference(offsetSpan.Span))
            fixed (byte* buff = stackalloc byte[size])
            {
                var span = new Span<byte>(buff, size);
                for (int i = 0; i < size; i++)
                {
                    span[i] = src[i];
                }

                if (bigEndian)
                {
                    span.Reverse();                    
                }

                offset += size;
                return Unsafe.Read<T>(buff);
            }
        }

        private static void ReadArray(ref int offset, ref ReadOnlyMemory<byte> data, ref ReadOnlySpan<byte> span, bool bigEndian, Type type, ref object boxed, Type valueType, FieldInfo info, ref object value)
        {
            ArraySizeAttribute arraySizeAttribute;
            ArraySizeRefAttribute arraySizeRefAttribute;

            var size = 0;
            var size_type = ArraySizeType.Elements;

            if ((arraySizeAttribute = info?.GetCachedCustomAttribute<ArraySizeAttribute>()) != null)
            {
                size = arraySizeAttribute.Size;
                size_type = arraySizeAttribute.Type;
            }
            else if ((arraySizeRefAttribute = info?.GetCachedCustomAttribute<ArraySizeRefAttribute>()) != null)
            {
                size = (int)arraySizeRefAttribute.GetRef(boxed);
                size_type = arraySizeRefAttribute.Type;
            }

            var elementType = valueType.GetElementType();
            if (size == 0)
            {
                throw new InvalidOperationException("Can't read an array without a size specified!");
            }

            if (size_type == ArraySizeType.Bytes)
            {
                var def = Activator.CreateInstance(elementType);
                size /= Marshal.SizeOf(def);
            }

            var array = Array.CreateInstance(elementType, size);
            for (int i = 0; i < size; i++)
            {
                if (ReadValue(ref offset, ref data, ref span, bigEndian, type, ref boxed, elementType, null, ref value))
                {
                    array.SetValue(value, i);
                }
            }

            value = array;
        }

        private static unsafe void ReadFixedArray(ref int offset, ref ReadOnlyMemory<byte> data, ref ReadOnlySpan<byte> span, bool bigEndian, Type type, ref object boxed, FieldInfo info, ref object value, FixedBufferAttribute fixedAttr)
        {
            var elementType = fixedAttr.ElementType;
            var size = fixedAttr.Length;
            var fixedArray = info.GetValue(value);
            var array = Array.CreateInstance(elementType, size);
            var byteSize = 0u;

            for (var i = 0; i < size; i++)
            {
                var off = offset;
                if (ReadValue(ref off, ref data, ref span, bigEndian, type, ref boxed, elementType, null, ref value))
                {
                    array.SetValue(value, i);
                }

                byteSize += (uint)(off - offset);
                offset = off;
            }

            var destHandle = GCHandle.Alloc(fixedArray, GCHandleType.Pinned);
            var srcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            void* pDest = (void*)destHandle.AddrOfPinnedObject();
            void* pSource = (void*)srcHandle.AddrOfPinnedObject();

            Unsafe.CopyBlock(pDest, pSource, byteSize);

            destHandle.Free();
            srcHandle.Free();

            value = fixedArray;
        }
    }
}
