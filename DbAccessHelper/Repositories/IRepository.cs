using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReviDbStructure
{
    public interface IRepository<T>
    {
        void Insert(object parameter);
        void Update(object parameter, object condition = null);
    }
}
