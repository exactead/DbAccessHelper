using ReviDbStructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using ReviDbStructure.Data.RawData;

namespace ReviDbStructure
{
    public sealed class ProductInstructionRepository : Repository<RawProductInstruction>
    {
        public ProductInstructionRepository(IDbConnection connection, char parameterSymbol) : base(connection, parameterSymbol)
        {
        }

        public IEnumerable<ProductInstruction> CreateDefaultData()
        {
            // 期限設定状態フラグFのデータの取得
            using (var exec = new Connection(connection))
            {
                foreach (var record in exec.Select<RawProductInstruction>(new { 期限設定状態ﾌﾗｸﾞ = "F" })?
                    .RawDataMapping<RawProductInstruction, ProductInstruction>())
                    yield return record;
            }
        }

        /// <summary>
        /// DBより指定されたクエリでデータを取得します
        /// </summary>
        /// <param name="query">クエリ</param>
        /// <param name="parameter">クエリパラメータ</param>
        /// <returns>ProductInstructionマッピングデータ</returns>
        public IEnumerable<ProductInstruction> GetQueryDatas(string query, object parameter = null)
        {
            using (var exec = new Connection(connection))
            {
                foreach (var record in exec.Select<RawProductInstruction>(query, parameter)?
                    .RawDataMapping<RawProductInstruction, ProductInstruction>())
                    yield return record;
            }
        }

    }

}
