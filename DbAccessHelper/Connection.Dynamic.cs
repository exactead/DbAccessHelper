using DbAccessHelper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper
{
    public partial class Connection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<IEnumerable<dynamic>> YieldReaderDynamicAsync(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = await CreateCommandAsync(query, parameter, commandType).ConfigureAwait(false);
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);    // SequentialAccessにすることでコルーチン対応を出来るようにする

            List<DynamicRecord> records = new();
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                IDictionary<string, object> dictionary = new ExpandoObject();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader.GetValue(i));
                }

                records.Add(new DynamicRecord(dictionary));
            }
            return records;
        }

        public Task<IEnumerable<dynamic>> ExecuteReaderDynamicAsync(string query, object parameter = null, CommandType commandType = CommandType.Text) => YieldReaderDynamicAsync(query, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<dynamic> YieldReaderDynamic(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = CreateCommand(query, parameter, commandType);
            using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);    // SequentialAccessにすることでコルーチン対応を出来るようにする

            while (reader.Read())
            {
                IDictionary<string, object> dictionary = new ExpandoObject();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dictionary.Add(reader.GetName(i), reader.GetValue(i));
                }

                yield return new DynamicRecord(dictionary);
            }
        }

        public IEnumerable<dynamic> ExecuteReaderDynamic(string query, object parameter = null, CommandType commandType = CommandType.Text) => YieldReaderDynamic(query, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public dynamic ExecuteScalarDynamic(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            using var command = CreateCommand(query, parameter, commandType);
            return command.ExecuteScalar();
        }
    }
}
