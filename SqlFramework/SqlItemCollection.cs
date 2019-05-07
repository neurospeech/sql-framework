using SqlFramework.Query;
using System;
using System.Collections.Generic;

namespace SqlFramework
{
    public class SqlItemCollection : IQueryParts
    {
        readonly string prefix;
        readonly string separator;
        readonly string emptyValue;
        public SqlItemCollection(
            string prefix,
            string separator,
            string emptyValue)
        {
            this.emptyValue = emptyValue;
            this.separator = separator;
            this.prefix = prefix;
        }

        public void Add(SqlQuery query)
        {
            this.Items.Add(query);
        }

        private List<SqlQuery> Items = new List<SqlQuery>();

        IEnumerable<QueryPart> IQueryParts.Parts
        {
            get
            {
                if (Items.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(this.emptyValue))
                    {
                        return new QueryPart[] { this.emptyValue };
                    }
                    return new QueryPart[] { };
                }
                IEnumerable<QueryPart> Create()
                {
                    var en = this.Items.GetEnumerator();
                    if (en.MoveNext())
                    {
                        yield return prefix;
                        foreach (var item in en.Current.fragments)
                        {
                            yield return item;
                        }
                    }
                    while (en.MoveNext())
                    {
                        yield return separator;
                        foreach (var item in en.Current.fragments)
                        {
                            yield return item;
                        }
                    }
                }
                return Create();
            }
        }
    }
}
