using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper
{
    public interface IConnection : IDisposable
    {
        int Insert<T>(object parameter);
        int Insert(string table, object parameter);
        int Update<T>(object parameter, object condition);
        int Update(string table, object parameter, object condition);
        int Delete<T>(object condition);
        int Delete(string table, object condition);
        IEnumerable<T> Select<T>(object condition = null, bool isBindByName = false) where T : new();
        IEnumerable<T> Select<T>(string query, object condition = null, bool isBindByName = false) where T : new();
        int ExecuteNonQuery(string query, object parameter, CommandType commandType = CommandType.Text);
        dynamic ExecuteScalarDynamic(string query, object parameter, CommandType commandType = CommandType.Text);
        T ExecuteScalar<T>(string query, object parameter, CommandType commandType = CommandType.Text);
        IEnumerable<dynamic> ExecuteReaderDynamic(string query, object parameter, CommandType commandType = CommandType.Text);
        IEnumerable<IDataRecord> ExecuteReader(string query, object parameter, CommandType commandType = CommandType.Text, bool isBindByName = false);
        void TransactionComplete(bool isRollBack = false);
    }
}
