using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersistentQueue;
using FluentAssertions;
using System.Threading;
using SQLite;

namespace Tests
{
    [TestFixture]
    public class FilterQueueTests : CommonTests
    {
        public override IPersistantQueueFactory BuildFactory()
        {
            return new FilterQueue.Factory();
        }

        [Test]
        public override void InstanceTypeCheck()
        {
            var className = "FilterQueue";

            using (var queue = this.Create("InstanceTypeCheck_" + className))
            {
                Assert.IsInstanceOf(typeof(FilterQueue), queue);
            }

            using (var queue = this.CreateNew("InstanceTypeCheck_" + className))
            {
                Assert.IsInstanceOf(typeof(FilterQueue), queue);
            }

            using (var queue = this.CreateNew())
            {
                Assert.IsInstanceOf(typeof(FilterQueue), queue);
            }
        }
    }

    [TestFixture]
    public class StandardQueueTests : CommonTests
    {

        public override IPersistantQueueFactory BuildFactory()
        {
            return new Queue.Factory();
        }

        [Test]
        public override void InstanceTypeCheck()
        {
            var className = "Queue";

            using (var queue = this.Create("InstanceTypeCheck_" + className))
            {
                Assert.IsInstanceOf(typeof(Queue), queue);
            }

            using (var queue = this.CreateNew("InstanceTypeCheck_" + className))
            {
                Assert.IsInstanceOf(typeof(Queue), queue);
            }

            using (var queue = this.CreateNew())
            {
                Assert.IsInstanceOf(typeof(Queue), queue);
            }
        }
    }
}
