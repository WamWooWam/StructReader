using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WamWooWam.StructReader
{
    public static class Utils
    {
        private static ConcurrentDictionary<MemberInfo, Dictionary<Type, Attribute>> _attributeCache
            = new ConcurrentDictionary<MemberInfo, Dictionary<Type, Attribute>>();

        private static ConcurrentDictionary<Type, Dictionary<string, FieldInfo>> _fieldCache
             = new ConcurrentDictionary<Type, Dictionary<string, FieldInfo>>();

        static Utils()
        {
            //var asm = Assembly.GetEntryAssembly();
            //foreach (var type in asm.GetTypes().Where(t => t.IsValueType))
            //{
            //    CacheMemberAttributes(type);
            //    var fields = CacheTypeFields(type);
            //    foreach (var field in fields.Values)
            //    {
            //        CacheMemberAttributes(field);
            //    }
            //}
        }

        public static object GetPropValue(this object obj, string propName)
        {
            var nameParts = propName.Split('.');
            if (nameParts.Length == 1)
            {
                return obj.GetType().GetCachedFields()[propName].GetValue(obj);
            }

            var o = obj;

            foreach (var part in nameParts)
            {
                if (o == null) { return null; }

                var type = o.GetType();
                var info = type.GetCachedFields()[part];
                if (info == null) { return null; }

                o = info.GetValue(o);
            }

            return o;
        }

        public static Dictionary<string, FieldInfo> GetCachedFields(this Type type)
        {
            if (!_fieldCache.TryGetValue(type, out var fields))
            {
                return CacheTypeFields(type);
            }

            return fields;
        }

        private static Dictionary<string, FieldInfo> CacheTypeFields(Type type)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) // undocumented for now
                             .OrderBy(f => f.MetadataToken)
                             .ToDictionary(k => k.Name, v => v);
            _fieldCache.TryAdd(type, fields);

            return fields;
        }

        public static T GetCachedCustomAttribute<T>(this MemberInfo info) where T : Attribute
        {
            if (info == null)
                return null;

            if (!_attributeCache.TryGetValue(info, out var attributes))
            {
                attributes = CacheMemberAttributes(info);
            }

            return attributes.TryGetValue(typeof(T), out var a) ? (T)a : null;
        }

        private static Dictionary<Type, Attribute> CacheMemberAttributes(MemberInfo info)
        {
            if (info == null)
                return null;

            var attributes = info.GetCustomAttributes(true).ToDictionary(k => k.GetType(), v => (Attribute)v);
            _attributeCache.TryAdd(info, attributes);
            return attributes;
        }
    }
}
