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
        private readonly IDbConnection dbConnection;
        private readonly char parameterSymbol;
        private readonly IsolationLevel isolation;
        private IDbTransaction transaction;
        private bool isTransaction;

        /// <summary>
        ///
        /// </summary>
        /// <param name="dbConn"></param>
        /// <param name="parameterSymbol">Oracle=':'(default),SqlServer='@'</param>
        public Connection(IDbConnection dbConnection, char parameterSymbol = ':')
        {
            this.dbConnection = dbConnection;
            this.parameterSymbol = parameterSymbol;
            this.isTransaction = false;
        }

        public Connection(IDbConnection dbConnection, IsolationLevel level, char parameterSymbol = ':')
        {
            this.dbConnection = dbConnection;
            this.parameterSymbol = parameterSymbol;
            this.isolation = level;
            this.isTransaction = true;
        }

        #region CommonMethod
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDbCommand CreateCommand
            (string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false)
        {
            if (dbConnection.State != ConnectionState.Open) dbConnection.Open();
            if (transaction == null && isTransaction) transaction = dbConnection.BeginTransaction(isolation);    // トランザクションの開始、そのまま使える場合は継続して使用

            var command = this.dbConnection.CreateCommand();
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

            if (transaction != null) command.Transaction = transaction;

            return command;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TransactionComplete(bool isRollBack = false)
        {
            if (transaction == null) return;
            if (transaction.Connection == null) return;
            try
            {
                if (isRollBack) transaction.Rollback();
                else transaction.Commit();
            }
            finally
            {
                transaction.Dispose();
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<IDataRecord> YieldReader
            (string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false)
        {
            using var command = CreateCommand(query, parameter, commandType, isBindByName);
            using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
            while (reader.Read()) yield return reader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ExecuteNonQuery(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = CreateCommand(query, parameter, commandType);
            return command.ExecuteNonQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<IDataRecord> ExecuteReader(string query, object parameter = null, CommandType commandType = CommandType.Text, bool isBindByName = false) => YieldReader(query, parameter, commandType, isBindByName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ExecuteScalar<T>(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = CreateCommand(query, parameter, commandType);
            return (T)command.ExecuteScalar();
        }

        /// <summary>
        /// 型TのTableAttributeに指定されたテーブルへInsert処理を行います
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Insert<T>(object parameter) =>
            // tablenameをAttributeより取得
            Insert(GetTableName<T>(), parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Insert(string table, object parameter)
        {
            if (string.IsNullOrEmpty(table)) return 0;
            var param = CreateKeyValueParameter(parameter);

            string query = "insert into " + table + " (" + CreateInsertColumnsPhrase(param) + ") values (" + CreateInsertValuesPhrase(param) + ")";
            return ExecuteNonQuery(query, CreateBindParameter(param, null));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> Select<T>(object condition = null, bool isBindByName = false) where T : new()
        {
            foreach (var item in Select<T>(string.Empty, condition, isBindByName))
            {
                yield return item;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> Select<T>(string query, object condition = null, bool isBindByName = false) where T : new()
        {
            var que = GenerateQuery<T>(query, condition);

            // ValueTupleにおける動作保証のため、型に問題がないか確認
            if (IsValueTuple<T>())
            {
                return SelectValueTuple<T>(que, isBindByName);
            }

            // ColumnAttributeよりマッピング表を設定
            var map = GetCustomColumnMap<T>();

            // バインド変数を作成し、そのまま取得する
            return ExecuteReader(que.Query, CreateBindParameter(que.Condition, null), isBindByName: isBindByName)
                .Select(dr =>
                {
                    // マッピング表を用いて、紐付けに問題がなければ値を格納
                    object obj = new T();
                    SetValueAction(dr, map, (T)obj).Invoke();
                    return (T)obj;
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<T> SelectValueTuple<T>(Internal.QueryParameter queryParameter, bool isBindByName = false) where T : new()
        {
            // ValueTupleを形成している型、コンストラクタを取得する
            (IEnumerable<System.Reflection.ConstructorInfo> constructorInfos, IEnumerable<Type[]> types) constructors = (null, null);

            constructors = GenerateValueTupleConstructorsInfo(typeof(T));
            // バインド変数を作成し、そのまま取得する
            return ExecuteReader(queryParameter.Query, CreateBindParameter(queryParameter.Condition, null), isBindByName: isBindByName)
                .Select(dr =>
                {
                    object obj = new T();
                    // コンストラクタ情報に基づき、生成に必要な値をレコード情報から取得する
                    var values = GenerateValueTupleConstructorArgsValue(constructors.types, dr);
                    SetValueToValueTuple(constructors.constructorInfos, values, ref obj);
                    return (T)obj;
                });
        }

        Action SetValueAction<T>(IDataRecord dr, Internal.KeyMapper map, T obj)
        {
            return new Action(() =>
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    // DBNullの場合は特に指定しないため、スキップ
                    if (dr.IsDBNull(i)) continue;
                    var info = map[dr.GetName(i)];
                    if (info is not null) SetValue(info, obj, dr[i]);
                }
            });
        }

        /// <summary>
        /// 型TのTableAttributeに指定されたテーブルへUpdate処理を行います。
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Update<T>(object parameter, object condition) =>
            // tablenameをAttributeより取得
            Update(GetTableName<T>(), parameter, condition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Update(string table, object parameter, object condition)
        {
            if (string.IsNullOrEmpty(table)) return 0;

            var param = CreateKeyValueParameter(parameter);
            var cond = CreateKeyValueParameter(condition);

            string query = $"update " + table + " set " + CreateParameterPhrase(param) + " " + CreateWherePhrase(cond);

            return ExecuteNonQuery(query, CreateBindParameter(param, cond));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Delete<T>(object condition) =>
            // tablenameをAttributeより取得
            Delete(GetTableName<T>(), condition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Delete(string table, object condition)
        {
            if (string.IsNullOrEmpty(table)) return 0;

            var cond = CreateKeyValueParameter(condition);

            string query = "delete from " + table + " " + CreateWherePhrase(cond);

            return ExecuteNonQuery(query, CreateBindParameter(null, cond));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (dbConnection is null) return;

            // トランザクションが途中の場合、ロールバックをかける
            if (transaction?.Connection is not null)
            {
                TransactionComplete(true);
            }

            dbConnection.Dispose();
        }

        private bool IsValueTuple<T>() => typeof(T).Name.Contains("ValueTuple`");
    }
}