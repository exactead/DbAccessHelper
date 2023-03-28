using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;

namespace DbAccessHelper
{
    public partial class Connection : IConnection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<DbCommand> CreateCommandAsync
            (string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false)
        {
            var connectionAsync = (DbConnection)dbConnection;
            if (connectionAsync.State != ConnectionState.Open) await connectionAsync.OpenAsync().ConfigureAwait(false);
            if (transaction == null && isTransaction) transaction = connectionAsync.BeginTransaction(isolation);    // トランザクションの開始、そのまま使える場合は継続して使用

            var command = connectionAsync.CreateCommand();
            command.CommandText = query;
            command.CommandType = commandType;

            if (isBindByName)
            {
                var prop = command.GetType().GetProperty("BindByName");
                if (prop != null) { prop.SetValue(command, true); }
            }

            var keys = parameter is ExpandoObject ? parameter as IDictionary<string, object> : CreateKeyValueParameter(parameter);

            foreach (var p in keys)
            {
                var param = command.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value;
                command.Parameters.Add(param);
            }

            if (transaction != null) command.Transaction = (System.Data.Common.DbTransaction)transaction;

            return command;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<IEnumerable<T>> YieldReaderAsync<T>
            (string query, Internal.KeyMapper map, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false) where T : new()
        {
            using var command = await CreateCommandAsync(query, parameter, commandType, isBindByName).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);
            List<T> list = new();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // マッピング表を用いて、紐付けに問題がなければ値を格納
                object obj = new T();
                SetValueAction(reader, map, (T)obj).Invoke();
                list.Add((T)obj);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<IEnumerable<T>> ExecuteReaderAsyncImpl<T>(string query, Internal.KeyMapper map, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false) where T : new()
            => YieldReaderAsync<T>(query, map, parameter, commandType, isBindByName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<IEnumerable<T>> YieldValueTupleReaderAsync<T>
            (string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false) where T : new()
        {
            using var command = await CreateCommandAsync(query, parameter, commandType, isBindByName).ConfigureAwait(false);
            var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);
            List<T> list = new();
            // ValueTupleを形成している型、コンストラクタを取得する
            (IEnumerable<System.Reflection.ConstructorInfo> constructorInfos, IEnumerable<Type[]> types) constructors = (null, null);
            constructors = GenerateValueTupleConstructorsInfo(typeof(T));
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                // マッピング表を用いて、紐付けに問題がなければ値を格納
                object obj = new T();
                // コンストラクタ情報に基づき、生成に必要な値をレコード情報から取得する
                var values = GenerateValueTupleConstructorArgsValue(constructors.types, reader);
                SetValueToValueTuple(constructors.constructorInfos, values, ref obj);
                list.Add((T)obj);
            }
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task<IEnumerable<T>> ExecuteValueTupleReaderAsyncImpl<T>(string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false) where T : new()
            => YieldValueTupleReaderAsync<T>(query, parameter, commandType, isBindByName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<int> ExecuteNonQueryAsyncImpl(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = await CreateCommandAsync(query, parameter, commandType);
            return await command.ExecuteNonQueryAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> ExecuteNonQueryAsync(string query, object parameter = null, CommandType commandType = CommandType.Text) => ExecuteNonQueryAsyncImpl(query, parameter, commandType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IEnumerable<T>> SelectAsync<T>(object condition = null, bool isBindByName = false) where T : new() => SelectAsync<T>(string.Empty, condition, isBindByName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<IEnumerable<T>> SelectAsync<T>(string query, object condition = null, bool isBindByName = false) where T : new() => SelectAsyncImpl<T>(query, condition, isBindByName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<IEnumerable<T>> SelectAsyncImpl<T>(string query, object condition = null, bool isBindByName = false) where T : new()
        {
            var que = GenerateQuery<T>(query, condition);
            // ValueTupleにおける動作保証のため、型に問題がないか確認
            if (IsValueTuple<T>())
            {
                return await ExecuteValueTupleReaderAsyncImpl<T>(que.Query, CreateBindParameter(que.Condition, null), isBindByName: isBindByName).ConfigureAwait(false);
            }
            // ColumnAttributeよりマッピング表を設定
            var map = GetCustomColumnMap<T>();

            // バインド変数を作成し、そのまま取得する
            return await ExecuteReaderAsyncImpl<T>(que.Query, map, CreateBindParameter(que.Condition, null), isBindByName: isBindByName).ConfigureAwait(false);
        }

        /// <summary>
        /// 型TのTableAttributeに指定されたテーブルへUpdate処理を行います。
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> UpdateAsync<T>(object parameter, object condition) =>
            // tablenameをAttributeより取得
            UpdateAsync(GetTableName<T>(), parameter, condition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> UpdateAsync(string table, object parameter, object condition)
        {
            if (string.IsNullOrEmpty(table)) return Task.FromResult(0);

            var param = CreateKeyValueParameter(parameter);
            var cond = CreateKeyValueParameter(condition);

            string query = $"update " + table + " set " + CreateParameterPhrase(param) + " " + CreateWherePhrase(cond);

            return ExecuteNonQueryAsync(query, CreateBindParameter(param, cond));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> DeleteAsync<T>(object condition) =>
            // tablenameをAttributeより取得
            DeleteAsync(GetTableName<T>(), condition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> DeleteAsync(string table, object condition)
        {
            if (string.IsNullOrEmpty(table)) return Task.FromResult(0);

            var cond = CreateKeyValueParameter(condition);

            string query = "delete from " + table + " " + CreateWherePhrase(cond);

            return ExecuteNonQueryAsync(query, CreateBindParameter(null, cond));
        }

    }
}