using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbAccessHelper
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute(string name = "", int orderNumber = 0, bool isDesc = false)
        {
            this.Name = name;
            this.Number = orderNumber;
            this.IsDesc = isDesc;
        }
        public string Name { get; }
        public int Number { get; }
        public bool IsDesc { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class IgnoreBindAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string name) => this.Name = name;
        public string Name { get; }
    }
}