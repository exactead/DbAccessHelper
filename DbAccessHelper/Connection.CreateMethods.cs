using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Data;
using System.Reflection;
using DbAccessHelper.Internal;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DbAccessHelper
{
    public sealed partial class Connection
    {
        bool isConditionParameterIncludedListOrArray = false;
        List<string> conditionParameterIncludedListOrArrayKeyList = new();
        // 匿名型で指定されたプロパティをDictionaryとして変換し生成する
        private IDictionary<string, object> CreateKeyValueParameter(object parameter = null)
        {
            if (parameter is null) return new Dictionary<string, object>();
            var param = GetPropertyInfos(parameter);
            if (!param.Where(x => !(x.PropertyType.IsPrimitive || x.PropertyType == typeof(string)))
                .Any(x => IsArrayOrGenericEnumerable(x))) return param.ToDictionary(v => v.Name, v => v.GetValue(parameter));

            return CreateKeyValueWithInPhraseParameter(parameter);
        }

        // 匿名型中に配列、Listが含まれるプロパティをDictionaryとして変換し生成する
        private IDictionary<string, object> CreateKeyValueWithInPhraseParameter(object parameter = null)
        {
            var param = parameter.GetType().GetProperties().Where(x => x.CanRead);
            isConditionParameterIncludedListOrArray = true;
            // IEnumerableのobjectを分割
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            foreach (var item in param)
            {
                var value = item.GetValue(parameter);
                if (item.PropertyType.IsPrimitive || item.PropertyType == typeof(string) || !IsArrayOrGenericEnumerable(item))
                {
                    dictionary.Add(item.Name, value);
                    continue;
                }
                int count = 0;
                conditionParameterIncludedListOrArrayKeyList.Add(item.Name);
                foreach (var v in value as IEnumerable)
                {
                    dictionary.Add($"{item.Name}_ListItem{count}", v);
                    count++;
                }
            }

            return dictionary;
        }

        IEnumerable<PropertyInfo> GetPropertyInfos(object parameter) => parameter.GetType().GetProperties().Where(x => x.CanRead);

        bool IsArrayOrGenericEnumerable(PropertyInfo propertyInfo) =>
            propertyInfo.PropertyType.GetInterfaces()
                .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        private OrderValueParameterMap CreateOrderValueParameter<T>()
        {
            var column = typeof(T).GetProperties().Where(p => p.CanRead)
            .Select(x => (x.Name, x.GetCustomAttributes(false).OfType<ColumnAttribute>().FirstOrDefault()))
            .Where(x => x.Item2?.Number > 0)
            .OrderBy(x => x.Item2.Number)
            .Select(x => new OrderValueParameter(x.Item2.Number,
                x.Item2.IsDesc ? SortAscending.Descending : SortAscending.Ascending,
                string.IsNullOrEmpty(x.Item2.Name) ? x.Name : x.Item2.Name)
                ).ToArray();

            return new OrderValueParameterMap(column);
        }
        private KeyMapper GetCustomColumnMap<T>() => new MemberAccessMap<T>().Create();

        private KeyMapper GetCustomColumnNullMap<T>() => new MemberAccessMap<T>().CreateNullMap();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateBindParameter(IDictionary<string, object> parameter = null, IDictionary<string, object> condition = null)
        {
            // ExpandoObjectを作成し、匿名型オブジェクトを生成する
            IDictionary<string, object> expando = new ExpandoObject();

            if (parameter != null)
            {
                foreach (var p in parameter)
                {
                    expando.Add(p.Key, p.Value == DBNull.Value ? null : p.Value);
                }
            }

            if (condition != null)
            {
                foreach (var c in condition.Select((val, idx) => new { val, idx }))
                {
                    if (c.val.Value == null || c.val.Value == DBNull.Value) continue;
                    expando.Add("wp_param" + c.idx, c.val.Value == DBNull.Value ? null : c.val.Value);
                }
            }

            return expando;
        }

        private string GetTableName<T>() => typeof(T).GetCustomAttributes(false).OfType<TableAttribute>().FirstOrDefault()?.Name;

        private string CreateParameterPhrase(IDictionary<string, object> parameter) => parameter.Any() ? string.Join(",", parameter.Select(name => $"{name.Key} = {parameterSymbol}{name.Key}")) : string.Empty;

        private string CreateOrderByPhrase(OrderValueParameterMap orderParameter)
        {
            if (!orderParameter.Any()) return "";
            return " order by " + string.Join(", ", orderParameter.OrderBy(x => x.OrderNumber).Select(x => $"{x.ColumnName} {(x.OrderSort == SortAscending.Descending ? "desc" : "asc")}"));
        }

        // 別パラメータにしないとSQLとして成立しない wp=where phrase
        private string CreateWherePhrase(IDictionary<string, object> condition)
        {
            if (!condition.Any()) return string.Empty;
            if (isConditionParameterIncludedListOrArray)
            {
                List<string> inPhrases = new();
                foreach (var k in conditionParameterIncludedListOrArrayKeyList)
                {
                    var arrayparam = condition.Where(c => c.Key.Contains($"{k}_ListItem"));
                    //inPhrases.Add($"{k} in " + string.Join(",", arrayparam.Select((wp, idx) => $"{parameterSymbol}{wp.Key}")));
                    inPhrases.Add($"{k} in {parameterSymbol}{k}");
                }

                return
                "where " + string.Join(" and ", condition.Where(c => !c.Key.Contains($"_ListItem"))
                        .Select((wp, idx) => wp.Value is null ? $"{wp.Key} is null" : $"{wp.Key} = {parameterSymbol}wp_param{idx}")
                        .Union(inPhrases));
            }
            else
            {
                return
                "where " + string.Join(" and ", condition
                        .Select((wp, idx) => wp.Value is null ? $"{wp.Key} is null" : $"{wp.Key} = {parameterSymbol}wp_param{idx}"));
            }
        }
        private string CreateInsertColumnsPhrase(IDictionary<string, object> paramater) => string.Join(",", paramater.Select(name => name.Key));

        private string CreateInsertValuesPhrase(IDictionary<string, object> paramater) => string.Join(",", paramater.Select(name => parameterSymbol + name.Key));

        private string CreateSelectQuery<T>(IDictionary<string, object> condition = null)
        {
            // tablenameをAttributeより取得
            string table = GetTableName<T>();

            // Where条件を作成し、SQLを生成
            return "select * from " + table + " " + CreateWherePhrase(condition);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(MemberInfo info, object obj, object value)
        {
            // レコードの値についてキャストを行う（値型はボックス化必須）ため、
            // クラスのフィールドがNullable型の場合は格納できるように基の型を渡すようにする
            if (info.MemberType == MemberTypes.Property)
            {
                var prop = info as PropertyInfo;
                var convertType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (!prop.CanWrite) return;

                if (convertType.IsEnum && decimal.TryParse(value.ToString(), out var number)) prop.SetValue(obj, GetSetValue(convertType, decimal.ToInt32(number), TypePattern.EnumDecimalValue));
                else if (convertType.IsEnum) prop.SetValue(obj, GetSetValue(convertType, value.ToString(), TypePattern.Enum));
                else prop.SetValue(obj, GetSetValue(convertType, value, TypePattern.Common));
            }
            else if (info.MemberType == MemberTypes.Field)
            {
                var field = info as FieldInfo;
                var convertType = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;

                if (convertType.IsEnum && decimal.TryParse(value.ToString(), out var number)) field.SetValue(obj, GetSetValue(convertType, decimal.ToInt32(number), TypePattern.EnumDecimalValue));
                else if (convertType.IsEnum) field.SetValue(obj, GetSetValue(convertType, value.ToString(), TypePattern.Enum));
                else field.SetValue(obj, GetSetValue(convertType, value, TypePattern.Common));
            }
        }

        enum TypePattern
        {
            Common,
            EnumDecimalValue,
            Enum
        }

        private object GetSetValue(Type type, object value, TypePattern pattern)
        {
            if (pattern == TypePattern.EnumDecimalValue) return Enum.ToObject(type, value);
            else if (pattern == TypePattern.Enum) return Enum.Parse(type, value.ToString());
            else return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValueToValueTuple(IEnumerable<ConstructorInfo> constructors, IEnumerable<object[]> itemValueList, ref object obj)
        {
            // Restにしているものから作成
            // 最終的に生成したものが返り値になる
            var value = (object)null;
            var restValue = (object)null;
            for (int i = constructors.Count() - 1; i >= 0; i--)
            {
                var constructor = constructors.ElementAt(i);
                // 1: ValueTuple<V1～V7> => new ValueTuple() 
                var arguments = itemValueList.ElementAt(i);
                if (restValue != null)
                {
                    // 2: ValueTuple<V1～V7、VRest> => new ValueTuple(new ValueTuple())
                    arguments = arguments.Append(restValue).ToArray();
                }
                value = Convert.ChangeType(constructor.Invoke(arguments), constructor.DeclaringType);
                restValue = value;
            }

            obj = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<object[]> GenerateValueTupleConstructorArgsValue(IEnumerable<Type[]> fieldTypes, IDataRecord dataRecord)
        {
            // 保証するものはValueTupleOnlyのとき

            // ListのListにすることでRest以降の個別のコンストラクタを作成
            // => Restの数分objectをnewする
            int index = 0;

            foreach (var types in fieldTypes)
            {
                var values = new object[types.Length];

                for (int i = 0; i < types.Length; i++)
                {
                    if (dataRecord.IsDBNull(index)) continue;
                    var dataRecordValue = dataRecord.GetValue(index);
                    if (types[i].IsEnum && decimal.TryParse(dataRecordValue.ToString(), out var number))
                    {
                        values[i] = GetSetValue(types[i], decimal.ToInt32(number), TypePattern.EnumDecimalValue);
                    }
                    else if (types[i].IsEnum)
                    {
                        values[i] = GetSetValue(types[i], dataRecordValue.ToString(), TypePattern.Enum);
                    }
                    else if (types[i].IsPrimitive || types[i] == typeof(string))
                    {
                        values[i] = GetSetValue(types[i], dataRecordValue, TypePattern.Common);
                    }
                    else if (types[i].IsClass)
                    {
                        // 通常のやり方同様、KeyMapperを作成し、値を格納していく
                        object obj = Activator.CreateInstance(types[i]);
                        var mapper = new MemberAccessMap().Create(types[i]);

                        for (int j = 0; j < mapper.Count; j++)
                        {
                            // DBNullの場合は特に指定しないため、スキップ
                            if (dataRecord.IsDBNull(index + j)) continue;
                            var info = mapper[dataRecord.GetName(index + j)];
                            if (info != null) SetValue(info, obj, dataRecord[index + j]);
                        }
                        index += mapper.Count - 1;
                        values[i] = obj;
                    }
                    else
                    {
                        values[i] = GetSetValue(types[i], dataRecordValue, TypePattern.Common);
                    }

                    index++;
                }

                yield return values;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (IEnumerable<ConstructorInfo>, IEnumerable<Type[]>) GenerateValueTupleConstructorsInfo(Type valueTupleType)
        {
            var constructors = new List<ConstructorInfo>();
            var currentType = valueTupleType;
            var constructorFieldInfos = new List<Type[]>();
            while (true)
            {
                var restField = (FieldInfo)null;
                var valueTupleFields = currentType.GetFields(BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                var arity = valueTupleFields.Any(x => x.Name == "Rest") ? valueTupleFields.Length - 1 : valueTupleFields.Length;
                var valueTupleFieldTypes = new Type[arity];

                for (int i = 0; i < valueTupleFields.Length; i++)
                {
                    var info = valueTupleFields[i];
                    if (info.Name == "Rest")
                    {
                        restField = info;
                    }
                    else if (info.Name.StartsWith("Item"))
                    {
                        var convertType = Nullable.GetUnderlyingType(info.FieldType) ?? info.FieldType;
                        valueTupleFieldTypes[i] = convertType;
                    }
                }

                var constructorArgs = restField != null ? valueTupleFieldTypes.Append(restField.FieldType).ToArray() : valueTupleFieldTypes;
                constructors.Add(currentType.GetConstructor(constructorArgs));
                constructorFieldInfos.Add(valueTupleFieldTypes);
                if (restField is null) break;
                currentType = restField.FieldType;
            }

            return (constructors, constructorFieldInfos);
        }

        private Internal.QueryParameter GenerateQuery<T>(string query, object condition) where T : new()
        {
            // Where条件のマップを作成（Dictionary）
            var cond = CreateKeyValueParameter(condition);
            // クエリがない場合はクラス情報より作成
            if (string.IsNullOrWhiteSpace(query))
            {
                // Order条件を作成し、SQLを生成
                var order = CreateOrderValueParameter<T>();
                query = CreateSelectQuery<T>(cond) + CreateOrderByPhrase(order);
            }

            // Where条件のパラメータが配列（リスト）形式か確認
            if (isConditionParameterIncludedListOrArray)
            {
                // trueであれば、IN句があるため、parameterSymbol+対象のキーによる文字の置換を行う
                return new QueryParameter(query, cond, conditionParameterIncludedListOrArrayKeyList);
            }

            return new QueryParameter(query, cond);
        }
    }
}
