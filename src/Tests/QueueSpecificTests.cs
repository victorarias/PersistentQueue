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
        public override IPersistentQueueFactory BuildFactory()
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

        [Test]
        public void FilterCountCorrectness()
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

                //All active items and all items should both contain the same entries as entities
                var activeItems = queue.ActiveItems();
                activeItems.Count.Should().Be(entities.Length);
                queue.AllItems().Count.Should().Be(entities.Length);

                //Soft delete
                queue.Delete(activeItems[1]);

                //Active items should be less one
                queue.ActiveItems().Count.Should().Be(entities.Length - 1);
                //All items should be untouched
                queue.AllItems().Count.Should().Be(entities.Length);
                //Deleted items should have one
                queue.DeletedItems().Count.Should().Be(1);

                //Hard delete
                queue.Delete(activeItems[1],true);

                //Active items remain unchanged
                queue.ActiveItems().Count.Should().Be(entities.Length - 1);
                //All items now matches active
                queue.AllItems().Count.Should().Be(entities.Length - 1);
                //Deleted items are zero
                queue.DeletedItems().Count.Should().Be(0);
            }
        }

        [Test]
        public void CanHardDeleteMultipleTimes()
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

                //All active items and all items should both contain the same entries as entities
                var activeItems = queue.ActiveItems();

                //Hard delete
                for (int cnt = 0; cnt < 5; cnt++)
                {
                    queue.Delete(activeItems[1], true);
                }
            }
        }
    }

    [TestFixture]
    public class StandardQueueTests : CommonTests
    {

        public override IPersistentQueueFactory BuildFactory()
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
