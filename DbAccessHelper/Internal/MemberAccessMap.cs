using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper.Internal
{
    internal class MemberAccessMap<T>
    {
        public KeyMapper Create() => new KeyMapper(GetColumnMap());
        public KeyMapper CreateNullMap() => new KeyMapper(new Dictionary<string, MemberInfo>());

        private IDictionary<string, MemberInfo> GetColumnMap() => 
            GetPropertyColumnMap().Union(GetFieldColumnMap())
                .ToDictionary(info =>
                {
                    if (info is PropertyInfo)
                    {
                        var attr = info.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .FirstOrDefault();
                        if (attr is null) return info.Name;
                        return string.IsNullOrEmpty(attr.Name) ? info.Name : attr.Name;
                    }

                    return info.Name;
                }, info => info);        

        // プロパティとして「ColumnAttribute」を検索条件として探す
        // ない場合は、プロパティ名称を渡す
        // カラム情報に一致するものを取得する
        private IEnumerable<MemberInfo> GetPropertyColumnMap() => 
            typeof(T).GetProperties().Where(p => p.CanRead).Where(p => p.CanWrite).Where(p => !p.GetCustomAttributes<IgnoreBindAttribute>().Any());
        
        // カラム情報に一致するものを取得する
        private IEnumerable<MemberInfo> GetFieldColumnMap() =>
            typeof(T).GetFields(BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Instance).Where(f => !f.GetCustomAttributes<IgnoreBindAttribute>().Any());

    }

    internal class MemberAccessMap
    {
        public KeyMapper Create(Type type) => new KeyMapper(GetColumnMap(type));
        public KeyMapper CreateNullMap() => new KeyMapper(new Dictionary<string, MemberInfo>());

        private IDictionary<string, MemberInfo> GetColumnMap(Type type) =>
            GetPropertyColumnMap(type).Union(GetFieldColumnMap(type))
                .ToDictionary(info =>
                {
                    if (info is PropertyInfo)
                    {
                        var attr = info.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .FirstOrDefault();
                        if (attr is null) return info.Name;
                        return string.IsNullOrEmpty(attr.Name) ? info.Name : attr.Name;
                    }

                    return info.Name;
                }, info => info);

        // プロパティとして「ColumnAttribute」を検索条件として探す
        // ない場合は、プロパティ名称を渡す
        // カラム情報に一致するものを取得する
        private IEnumerable<MemberInfo> GetPropertyColumnMap(Type type) =>
            type.GetProperties().Where(p => p.CanRead).Where(p => p.CanWrite).Where(p => !p.GetCustomAttributes<IgnoreBindAttribute>().Any());

        // カラム情報に一致するものを取得する
        private IEnumerable<MemberInfo> GetFieldColumnMap(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Instance).Where(f => !f.GetCustomAttributes<IgnoreBindAttribute>().Any());
    }
}
