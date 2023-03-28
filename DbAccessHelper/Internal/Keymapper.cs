using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper.Internal
{
    internal class KeyMapper
    {
        readonly IDictionary<string, MemberInfo> Cache = new Dictionary<string, MemberInfo>();

        public KeyMapper(IDictionary<string, MemberInfo> source) => Cache = source;

        public MemberInfo this[string key] => Cache.TryGetValue(key, out var value) ? value : default;

        public int Count => Cache.Count;

        public bool HasKey => Cache.Keys.Any();
    }
}
