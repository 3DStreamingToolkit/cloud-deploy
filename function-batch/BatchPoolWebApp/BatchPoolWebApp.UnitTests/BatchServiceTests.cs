using System;
using Xunit;
using BatchPoolWebApp.Services;

namespace BatchPoolWebApp.UnitTests
{
    public class BatchServiceTests
    {
        [Fact]
        public void TestBatchPoolCreation()
        {
            var someCondition = true;
            Assert.True(someCondition);
        }

        [Theory]
        [InlineData("SomeValue")]
        public void TestBatchPoolCreationWithParams(string someParam)
        {
            Assert.Equal("SomeValue",someParam);
        }
    }
}
