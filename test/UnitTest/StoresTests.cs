using Microsoft.Extensions.DependencyInjection;
using N4pper;
using System;
using System.Linq.Expressions;
using Xunit;

namespace UnitTest
{
    [TestCaseOrderer(AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeName, AsIKnow.XUnitExtensions.Constants.PriorityOrdererTypeAssemblyName)]
    [Collection(nameof(Neo4jCollection))]
    public class StoresTests
    {
        protected Neo4jFixture Fixture { get; set; }

        public StoresTests(Neo4jFixture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public void Test1()
        {

        }
    }
}
