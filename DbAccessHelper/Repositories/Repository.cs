using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviDbStructure
{
    public abstract class Repository<T> : IRepository<T> where T : class
    {
        protected IDbConnection connection { get; set; }
        protected char parameterSymbol { get; set; }

        protected Repository(IDbConnection connection, char parameterSymbol)
        {
            this.connection = connection;
            this.parameterSymbol = parameterSymbol;
        }

        internal IConnection CreateDbConnection(IDbConnection connection)
        {
            return new Connection(connection, parameterSymbol);
        }

        internal IConnection CreateDbConnection(IDbConnection connection, IsolationLevel level)
        {
            return new Connection(connection, level, parameterSymbol);
        }

        public void Insert(object parameter)
        {
            using (var connection = this.CreateDbConnection(this.connection, IsolationLevel.ReadCommitted))
            { 
                connection.Insert<T>(parameter);
                connection.TransactionComplete();
            }
        }

        public void Update(object parameter, object condition = null)
        {
            using (var connection = this.CreateDbConnection(this.connection, IsolationLevel.ReadCommitted))
            {
                connection.Update<T>(parameter, condition);
                connection.TransactionComplete();
            }
        }
    }

    public class RepositoryFactory
    {
        public RepositoryFactory()
        {
        }

        private static PartArrangementOrderRepository parts { get; set; }
        private static PartArrangementOrderRepository boms { get; set; }
        private static RequiredDrawingRepository drawings { get; set; }
        private static ProductInstructionRepository products { get; set; }

        public static void CreateRepository(IDbConnection connection, char parameterSymbol = ':')
        {
            // 各種リポジトリを作成
            parts = new PartArrangementOrderRepository(connection, parameterSymbol);
            drawings = new RequiredDrawingRepository(connection, parameterSymbol);
            products = new ProductInstructionRepository(connection, parameterSymbol);
        }

        public static PartArrangementOrderRepository GetPartArrangementOrderRepository()
        {
            return parts;
        }

        public static ProductInstructionRepository GetProductInstructionRepository()
        {
            return products;
        }

        public static RequiredDrawingRepository GetRequiredDrawingRepository()
        {
            return drawings;
        }
    }
}
