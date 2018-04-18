using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace UnitTest
{
    [CollectionDefinition(nameof(Neo4jCollection))]
    public class Neo4jCollection : ICollectionFixture<Neo4jFixture>
    {
    }
}
