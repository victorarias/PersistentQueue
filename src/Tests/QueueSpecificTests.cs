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

        [Test]
        public void DeleteTimeShouldBeRespected()
        {
            using (var queue = this.CreateNew() as FilterQueue)
            {
                var entities = new[]{
                    "One",
                    "Two",
                    "Skipped",
                    "Three"
                };

                foreach (var entity in entities)
                {
                    queue.Enqueue(entity);
                }

                var activeItems = queue.ActiveItems();

                activeItems.Count.Should().Be(4);

                queue.Delete(activeItems[2]);

                activeItems = queue.ActiveItems();

                activeItems.Count.Should().Be(3);

                var deletedItems = queue.DeletedItems();

                deletedItems.Count.Should().Be(1);

                var item1 = queue.Dequeue().CastTo<String>();
                var item2 = queue.Dequeue().CastTo<String>();
                var item3 = queue.Dequeue().CastTo<String>();

                item1.Should().Be(entities[0]);
                item2.Should().Be(entities[1]);
                item3.Should().Be(entities[3]);

                //Still available
                deletedItems = queue.DeletedItems();

                deletedItems.Count.Should().Be(4);

                queue.PurgeDeletedItems();

                deletedItems = queue.DeletedItems();

                deletedItems.Count.Should().Be(0);
            }
        }

        [Test]
        public void HardDeletesShouldBeRespected()
        {
            using (var queue = this.CreateNew() as FilterQueue)
            {
                var entities = new[]{
                    "One",
                    "Two",
                    "Three"
                };

                foreach (var entity in entities)
                {
                    queue.Enqueue(entity);
                }

                var activeItems = queue.ActiveItems();

                activeItems.Count.Should().Be(3);

                queue.Delete(activeItems[1], true);

                activeItems = queue.ActiveItems();

                activeItems.Count.Should().Be(2);

                queue.DeletedItems().Count.Should().Be(0);

                activeItems[0].CastTo<String>().Should().Be(entities[0]);
                activeItems[1].CastTo<String>().Should().Be(entities[2]);
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
