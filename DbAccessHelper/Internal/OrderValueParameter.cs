using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DbAccessHelper.Internal
{
    internal class OrderValueParameterMap : IEnumerable<OrderValueParameter>
    {
        public OrderValueParameterMap(IEnumerable<OrderValueParameter> Map)
        {
            this.Map = Map;
        }
        IEnumerable<OrderValueParameter> Map { get; set; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        IEnumerator<OrderValueParameter> IEnumerable<OrderValueParameter>.GetEnumerator()
        {
            return (IEnumerator<OrderValueParameter>)GetEnumerator();
        }

        public OrderValueParameterMapEnumerator GetEnumerator()
        {
            return new OrderValueParameterMapEnumerator(Map);
        }

    }
    internal class OrderValueParameterMapEnumerator : IEnumerator<OrderValueParameter>
    {
        int index = 0;
        public OrderValueParameterMapEnumerator(IEnumerable<OrderValueParameter> Map)
        {
            this.Map = Map;
        }
        IEnumerable<OrderValueParameter> Map;
        public void Dispose() { }
        public bool MoveNext()
        {
            index++;
            return index < Map.Count();
        }
        public void Reset() { index = 0; }

        OrderValueParameter IEnumerator<OrderValueParameter>.Current => Map.ElementAt(index);

        object IEnumerator.Current => Map.ElementAt(index);
    }

    internal class OrderValueParameter
    {
        public OrderValueParameter(int orderNumber, SortAscending sortAscending, string columnName)
        {
            this.OrderNumber = orderNumber;
            this.OrderSort = sortAscending;
            this.ColumnName = columnName;
        }
        public int OrderNumber { get; }
        public SortAscending OrderSort { get; }
        public string ColumnName { get; }
    }

    enum SortAscending
    {
        Ascending,
        Descending
    }
}