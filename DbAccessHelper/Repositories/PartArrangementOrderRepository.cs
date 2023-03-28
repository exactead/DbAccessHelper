using ReviDbStructure.Data;
using ReviDbStructure.Data.RawData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviDbStructure
{
    public sealed class PartArrangementOrderRepository : Repository<RawPartArrangementOrder>
    {
        public PartArrangementOrderRepository(IDbConnection connection, char parameterSymbol) : base(connection, parameterSymbol)
        {
        }

        /// <summary>
        /// DBより指定されたクエリでデータを取得します
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="parameter">クエリパラメータ</param>
        /// <returns>PartArrangementOrdersマッピングデータ</returns>
        public IEnumerable<PartArrangementOrder> GetQueryDatas(string query, object parameter = null)
        {
            using (var exec = new Connection(connection))
            {
                foreach (var record in exec.Select<RawPartArrangementOrder>(query, parameter)?
                    .RawDataMapping<RawPartArrangementOrder, PartArrangementOrder>())
                    yield return record; 
            }
        }

        /// <summary>
        /// DB内「部品手配経緯」より移行フラグを条件にしたデータを取得します
        /// </summary>
        public IEnumerable<PartArrangementOrder> GetConditionMigrationFlag(int? flag)
        {
            // 移行フラグ条件データの取得
            using (var exec = new Connection(connection))
            {
                foreach (var record in exec.Select<RawPartArrangementOrder>(new { 移行ﾌﾗｸﾞ = flag })?
                    .RawDataMapping<RawPartArrangementOrder, PartArrangementOrder>())
                    yield return record;
            }
        }
    }
}
