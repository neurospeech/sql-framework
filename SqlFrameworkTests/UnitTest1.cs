using SqlFramework.Query;
using System;
using System.Linq;
using Xunit;

namespace SqlFrameworkTests
{
    public class EFExtensionsTest
    {

        [Fact]
        public void FormatSqlTest()
        {

            var now = DateTime.UtcNow;
            SqlQuery sql1 = SqlQuery.New();

            sql1 += $"SELECT * FROM Invoices WHERE Invoices.Total > {20} AND Invoices.PaidDate < {now}";

            var first = sql1.Values[0];

            Assert.Equal("SELECT * FROM Invoices WHERE Invoices.Total > @p0 AND Invoices.PaidDate < @p1", sql1.Text);
            Assert.Equal("@p0", first.Key);
            Assert.Equal(20, first.Value);

            first = sql1.Values[1];
            Assert.Equal("@p1", first.Key);
            Assert.Equal(now, first.Value);

            sql1 += $" AND Invoices.TalentID = {5}";

            Assert.Equal("SELECT * FROM Invoices WHERE Invoices.Total > @p0 AND Invoices.PaidDate < @p1 AND Invoices.TalentID = @p2", sql1.Text);

            Assert.Equal("@p0", sql1.Values[0].Key);
            Assert.Equal("@p1", sql1.Values[1].Key);
            Assert.Equal("@p2", sql1.Values[2].Key);

            Assert.Equal(20, sql1.Values[0].Value);
            Assert.Equal(now, sql1.Values[1].Value);
            Assert.Equal(5, sql1.Values[2].Value);

        }

        [Fact]
        public void NestedTest()
        {
            SqlQuery join = SqlQuery.New($"join Invoices on (Invoices.TalentID = Talents.TalentID AND Invoices.Amount > {20} AND Invoices.Total < {30})");
            SqlQuery query = SqlQuery.New($"select * from talents {join} where talentid > {1000}");

            Assert.Equal("select * from talents join Invoices on (Invoices.TalentID = Talents.TalentID AND Invoices.Amount > @p0 AND Invoices.Total < @p1) where talentid > @p2", query.Text);
        }

        [Fact]
        public void NestedTest2()
        {
            SqlQuery top = SqlQuery.New($"top ({20})");
            SqlQuery join = SqlQuery.New($"join Invoices on (Invoices.TalentID = Talents.TalentID AND Invoices.Amount > {20} AND Invoices.Total < {30})");
            SqlQuery query = SqlQuery.New($"select {top} * from talents {join} where talentid > {1000}");

            Assert.Equal(
                "select top (20) * from talents join Invoices on (Invoices.TalentID = Talents.TalentID " +
                "AND Invoices.Amount > @p0 AND Invoices.Total < @p1) where talentid > @p2", query.Text);
        }

        [Fact]
        public void NestedTest3()
        {
            SqlQuery B = SqlQuery.New($"B");
            SqlQuery AB = SqlQuery.New($"A {B}");
            SqlQuery ABC = SqlQuery.New($"{AB} C");

            Assert.Equal("A B C", ABC.Text);
        }

        [Fact]
        public void NestedTest4()
        {
            SqlQuery B = SqlQuery.New($"B {2}");
            SqlQuery AB = SqlQuery.New($"A {1} {B}");
            SqlQuery ABC = SqlQuery.New($"{AB} C {3}");

            Assert.Equal("A @p0 B @p1 C @p2", ABC.Text);

            void AssertValue(string name, object value)
            {
                var a = ABC.Values.FirstOrDefault(x => x.Key == name);
                Assert.Equal(name, a.Key);
                Assert.Equal(value, a.Value);
            }

            AssertValue("@p0", 1);
            AssertValue("@p1", 2);
            AssertValue("@p2", 3);

        }
        [Fact]
        public void Multiple()
        {
            var q = SqlQuery.New(
                $"select * from customers",
                $" where customers.name = {"a"}",
                $" and customers.active = {true}");

            Assert.Equal("select * from customers where customers.name = @p0 and customers.active = @p1", q.Text);
            var p0 = q.Values[0];
            Assert.Equal("@p0", p0.Key);
            Assert.Equal("a", p0.Value);
            var p1 = q.Values[1];
            Assert.Equal("@p1", p1.Key);
            Assert.Equal(true, p1.Value);
        }
        [Fact]
        public void FragmentTest()
        {
            var fs = new SqlFragments(" and ");
            fs.Add($"a = {1}");
            fs.Add($"b = {2}");
            fs.Add(SqlQuery.New($"c = {3}"));

            var q = SqlQuery.New($"select * from customers where {fs}");

            Assert.Equal("select * from customers where a = @p0 and b = @p1 and c = @p2", q.Text);
        }
        [Fact]
        public void FragmentWithPrefixTest()
        {
            var fs = new SqlFragments("where ", " and ");
            fs.Add($"a = {1}");
            fs.Add($"b = {2}");
            fs.Add(SqlQuery.New($"c = {3}"));

            var q = SqlQuery.New($"select * from customers {fs}");

            Assert.Equal("select * from customers where a = @p0 and b = @p1 and c = @p2", q.Text);
        }
    }
}
