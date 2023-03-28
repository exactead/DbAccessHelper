using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbAccessHelper.Internal
{
    internal class QueryParameter
    {
        public QueryParameter(string query, IDictionary<string, object> condition, char parameterSymbol = ':')
        {
            this.Query = query;
            this.Condition = condition;
            this.parameterSymbol = parameterSymbol;
        }

        public QueryParameter(string query, IDictionary<string, object> condition, IEnumerable<string> inPhraseParameters, char parameterSymbol = ':')
        {
            this.Condition = condition;
            this.parameterSymbol = parameterSymbol;
            this.Query = ReplaceQueryInPharases(query, inPhraseParameters);
        }

        private string ReplaceQueryInPharases(string query, IEnumerable<string> keyList)
        {
            string temp = query;
            foreach (var key in keyList)
            {
                string keyvalue = string.Join(", ", Condition.Keys.Where(x => x.Contains($"{key}_ListItem")).Select(x => $"{parameterSymbol}{x}").ToArray());
                temp = Regex.Replace(temp, $@"\(?{parameterSymbol}{key}\)?", $"({keyvalue})");
            }
            return temp;
        }

        public IDictionary<string, object> Condition { get; }
        public string Query { get; }
        public char parameterSymbol { get; }
    }
}