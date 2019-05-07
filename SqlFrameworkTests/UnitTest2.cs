using SqlFramework;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SqlFrameworkTests
{
    
    public class UnitTest2
    {

        [Fact]
        public void Test1()
        {
            var query = new SqlSelect();

            query.Table.TableName = "products";

            string t = query.ToString();
            Assert.NotNull(t);
        }

    }
}
