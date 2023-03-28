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
    public sealed class RequiredDrawingRepository : Repository<RawRequiredDrawing>
    {
        public RequiredDrawingRepository(IDbConnection connection, char parameterSymbol) : base(connection, parameterSymbol)
        {
        }

        public IEnumerable<RequiredDrawing> GetQueryDatas(string query, object parameter = null)
        {
            // 様々なクエリに対応できるようにする
            using (var exec = new Connection(connection))
            {
                foreach (var record in exec.Select<RawRequiredDrawing>(query, parameter)?
                    .RawDataMapping<RawRequiredDrawing, RequiredDrawing>())
                    yield return record;
            }
        }



    }
}

