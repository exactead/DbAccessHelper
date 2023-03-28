using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper.DbAccess
{
    public class DbAccess
    {
        #region DynamicMethods

        static async Task<IEnumerable<dynamic>> ExecuteReaderDynamicAsyncHelper
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            return (await exec.ExecuteReaderDynamicAsync(query, condition, commandType).ConfigureAwait(false)).Select(record => record);
        }

        public static Task<IEnumerable<dynamic>> ExecuteReaderDynamicAsync
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            return ExecuteReaderDynamicAsyncHelper(connection, query, parameterSymbol, condition, commandType);
        }

        static IEnumerable<dynamic> ExecuteReaderDynamicHelper
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            foreach (var record in exec.ExecuteReaderDynamic(query, condition, commandType))
            {
                yield return record;
            }
        }

        public static IEnumerable<dynamic> ExecuteReaderDynamic
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            return ExecuteReaderDynamicHelper(connection, query, parameterSymbol, condition, commandType);
        }

        public static dynamic ExecuteScalarDynamic
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.ExecuteScalarDynamic(query, condition, commandType);
        }

        #endregion

        static IEnumerable<IDataRecord> ExecuteReaderHelper
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            foreach (var record in exec.ExecuteReader(query, condition, commandType))
            {
                yield return record;
            }
        }

        public static IEnumerable<IDataRecord> ExecuteReader
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
            => ExecuteReaderHelper(connection, query, parameterSymbol, condition, commandType);

        public static T ExecuteScalar<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.ExecuteScalar<T>(query, condition, commandType);
        }

        public static int ExecuteNonQuery
            (IDbConnection connection, string query, char parameterSymbol = ':', object parameter = null, CommandType commandType = CommandType.Text)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.ExecuteNonQuery(query, parameter, commandType);
        }

        #region GenericsMethods

        static IEnumerable<T> SelectHelper<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text, bool isBindByName = false)
            where T : new()
        {
            using Connection exec = new(connection, parameterSymbol);
            foreach (var record in exec.Select<T>(query, condition, isBindByName))
            {
                yield return record;
            }
        }

        static async Task<IEnumerable<T>> SelectAsyncHelper<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, CommandType commandType = CommandType.Text, bool isBindByName = false)
            where T : new()
        {
            using Connection exec = new(connection, parameterSymbol);
            return (await exec.SelectAsync<T>(query, condition, isBindByName).ConfigureAwait(false)).Select(record => record);
        }

        public static IEnumerable<T> Select<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, bool isBindByName = false) where T : new()
            => SelectHelper<T>(connection, query, parameterSymbol, condition, isBindByName: isBindByName);

        public static Task<IEnumerable<T>> SelectAsync<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, bool isBindByName = false) where T : new()
            => SelectAsyncHelper<T>(connection, query, parameterSymbol, condition, isBindByName: isBindByName);

        public static T SelectSingle<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, bool isBindByName = false) where T : new()
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.Select<T>(query, condition, isBindByName).FirstOrDefault();
        }

        private static async Task<T> SelectSingleAsyncHelper<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, bool isBindByName = false) where T : new()
        {
            using Connection exec = new(connection, parameterSymbol);
            return (await exec.SelectAsync<T>(query, condition, isBindByName)).FirstOrDefault();
        }

        public static Task<T> SelectSingleAsync<T>
            (IDbConnection connection, string query, char parameterSymbol = ':', object condition = null, bool isBindByName = false) where T : new()
            => SelectSingleAsyncHelper<T>(connection, query, parameterSymbol, condition, isBindByName);

        public static int Update
            (IDbConnection connection, string tableName, char parameterSymbol = ':', object parameter = null, object condition = null)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.Update(tableName, parameter, condition);
        }

        public static int Insert
            (IDbConnection connection, string tableName, char parameterSymbol = ':', object parameter = null)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.Insert(tableName, parameter);
        }

        public static int Delete
            (IDbConnection connection, string tableName, char parameterSymbol = ':', object condition = null)
        {
            using Connection exec = new(connection, parameterSymbol);
            return exec.Delete(tableName, condition);
        }
        #endregion
    }
}
