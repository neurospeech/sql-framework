using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlFramework.Query
{
    internal class SqlQueryFormattable : FormattableString
    {
        object[] values;
        public SqlQueryFormattable(SqlQuery query)
        {
            this.Format = query.ToString();
            this.values = query.fragments.Where(x => x.hasArgument).Select(x => x.argument).ToArray();
        }

        public override int ArgumentCount => values.Length;

        public override string Format { get; }

        public override object GetArgument(int index)
        {
            return values[index];
        }

        public override object[] GetArguments()
        {
            return values;
        }

        public override string ToString(IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, Format, GetArguments());
        }
    }

    public struct SqlFragments
    {
        private List<SqlQuery> fragments;
        private readonly string separator;
        private readonly string prefix;

        public SqlFragments(string separator)
        {
            this.prefix = null;
            this.separator = separator;
            this.fragments = new List<SqlQuery>();
        }

        public SqlFragments(string prefix, string separator)
        {
            this.prefix = prefix;
            this.separator = separator;
            this.fragments = new List<SqlQuery>();
        }

        public void Add(SqlQuery fragment)
        {
            this.fragments.Add(fragment);
        }
        public void Add(FormattableString fragment)
        {
            this.fragments.Add(fragment);
        }
        public SqlQuery ToSqlQuery()
        {
            return SqlQuery.Join(prefix, separator, fragments);
        }
    }

    public interface IQueryParts
    {
        IEnumerable<QueryPart> Parts { get; }
    }

    public struct QueryPart
    {
        public string literal;
        public bool hasArgument;
        public object argument;

        public QueryPart(string literal, bool hasArgument, object argument) : this()
        {
            this.literal = literal;
            this.hasArgument = hasArgument;
            this.argument = argument;
        }

        public static implicit operator QueryPart(string literal)
        {
            return new QueryPart(literal, false, null);
        }

    }

    public struct SqlQuery: IQueryParts
    {

        internal List<QueryPart> fragments;

        public static implicit operator SqlQuery(FormattableString sql)
        {
            return new SqlQuery(sql);
        }

        public SqlQuery(FormattableString sql)
            : this(sql.Format, sql.GetArguments())
        {
        }

        public static SqlQuery Join(string prefix, string separator, IEnumerable<SqlQuery> fragments)
        {
            var r = prefix != null ? SqlQuery.From(prefix) : SqlQuery.New();
            var e = fragments.GetEnumerator();
            if (e.MoveNext())
            {
                r.fragments.AddRange(e.Current.fragments);
            }
            while (e.MoveNext())
            {
                r.fragments.Add( new QueryPart(separator, false, null));
                r.fragments.AddRange(e.Current.fragments);
            }
            return r;
        }
        public static SqlQuery Join(string separator, IEnumerable<SqlQuery> fragments)
        {
            return Join(null, separator, fragments);
        }

        public static SqlQuery Join(string prefix, string separator, params FormattableString[] fragments)
        {
            return SqlQuery.Join(prefix, separator, fragments.Select(x => SqlQuery.New(x)));
        }

        public static SqlQuery Join(string separator, params FormattableString[] fragments)
        {
            return SqlQuery.Join(null, separator, fragments);
        }

        private SqlQuery(string text, object[] args)
        {
            fragments = new List<QueryPart>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            for (int i = 0; i < args.Length; i++)
            {
                var sep = $"{{{i}}}";
                var index = text.IndexOf(sep);

                var prefix = text.Substring(0, index);
                text = text.Substring(index + sep.Length);
                var arg = args[i];
                fragments.Add(new QueryPart(prefix, false, null));
                if (arg is IQueryParts q)
                {
                    fragments.AddRange(q.Parts);
                }
                else if (arg is SqlFragments sf)
                {
                    var qf = sf.ToSqlQuery();
                    fragments.AddRange(qf.fragments);
                }
                else if (!(arg is string) && arg is System.Collections.IEnumerable en)
                {
                    var e = en.GetEnumerator();
                    if (e.MoveNext())
                    {
                        fragments.Add(new QueryPart(null, true, e.Current));
                    }
                    while (e.MoveNext())
                    {
                        fragments.Add(new QueryPart(",", false, null));
                        fragments.Add(new QueryPart(null, true, e.Current));
                    }
                }
                else
                {
                    fragments.Add(new QueryPart(null, true, args[i]));
                }
            }
            fragments.Add(new QueryPart(text, false, null));

        }

        public FormattableString ToFormattableString()
        {
            return new SqlQueryFormattable(this);
        }

        public static SqlQuery operator +(SqlQuery first, FormattableString sql)
        {
            var r = new SqlQuery(sql);
            r.fragments.InsertRange(0, first.fragments);
            return r;
        }

        public static SqlQuery operator +(SqlQuery first, SqlQuery r)
        {
            r.fragments.InsertRange(0, first.fragments);
            return r;
        }

        public static SqlQuery New()
        {
            return new SqlQuery($"");
        }

        public static SqlQuery New(params FormattableString[] sql)
        {
            if (sql.Length == 0)
            {
                throw new ArgumentException("Atleast one query must be specified");
            }
            var q = SqlQuery.New();
            foreach (var s in sql)
            {
                q += s;
            }
            return q;
        }


        internal static SqlQuery From(string format, params object[] parameters)
        {
            return new SqlQuery(format, parameters);
        }

        public override string ToString()
        {
            int ix = 0;
            return string.Join("", this.fragments.Select((x, i) => x.hasArgument ? $"{{{ix++}}}" : x.literal));
        }

        public string Text
        {
            get
            {
                int ix = 0;
                return string.Join("", this.fragments.Select((x, i) => x.hasArgument ? $"@p{ix++}" : x.literal));
            }
        }

        public KeyValuePair<string, object>[] Values
        {
            get
            {
                int ix = 0;
                return this.fragments
                    .Where(x => x.hasArgument)
                    .Select(x => new KeyValuePair<string, object>(
                        $"@p{ix++}",
                        x.argument))
                    .ToArray();
            }
        }

        IEnumerable<QueryPart> IQueryParts.Parts => this.fragments;
    }
}
