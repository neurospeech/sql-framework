using SqlFramework.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlFramework
{
    public static class QueryExtensions
    {



    }

    public class SqlTable: IQueryParts
    {

        public string TableName { get; set; }

        IEnumerable<QueryPart> IQueryParts.Parts => new QueryPart[] { $"[{this.TableName}]" };
    }

    public class SqlSelect : IQueryParts
    {

        public SqlTable Table { get; } = new SqlTable();

        public SqlFieldList Fields { get; } = new SqlFieldList();

        public SqlFilter Filter { get; } = new SqlFilter();

        public SqlOrder OrderBy { get; } = new SqlOrder();

        IEnumerable<QueryPart> IQueryParts.Parts
        {
            get
            {
                var query = SqlQuery.New($"SELECT {Fields} FROM {Table} {Filter} {OrderBy}");
                return query.fragments;
            }
        }

        public override string ToString()
        {
            return SqlQuery.New($"{this}").ToString();
        }
    }
    public class SqlFieldList: SqlItemCollection
    {
        public SqlFieldList(): base(null, ", \r\n", " * ")
        {

        }
    }

    public class SqlFilter: SqlItemCollection
    {
        public SqlFilter(): base(" where ", " AND ", null)
        {

        }
    }

    public class SqlOrder: SqlItemCollection
    {
        public SqlOrder(): base(" order by ", ", ", null)
        {

        }
    }

}
