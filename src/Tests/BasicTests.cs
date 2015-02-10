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
    public class BasicTests
    {
        [Test]
        public void Covarience()
        {
            IPersistantQueue<PersistantQueueItem> queue;
            var queueFactory = new Queue.Factory();
            var filterQueueFactory = new FilterQueue.Factory();

            using (queue = queueFactory.CreateNew()) { }
            using (queue = filterQueueFactory.CreateNew()) { };
        }
    }

    [TestFixture]
    public class FilterQueueTests : CommonTests
    {
        FilterQueue.Factory factory = new FilterQueue.Factory();

        public override IPersistantQueue<PersistantQueueItem> Create(string queueName)
        {
            return factory.Create(queueName);
        }

        public override IPersistantQueue<PersistantQueueItem> CreateNew(string queueName)
        {
            return factory.CreateNew(queueName);
        }

        public override IPersistantQueue<PersistantQueueItem> CreateNew()
        {
            return factory.CreateNew();
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
        Queue.Factory factory = new Queue.Factory();

        public override IPersistantQueue<PersistantQueueItem> Create(string queueName)
        {
            return factory.Create(queueName);
        }

        public override IPersistantQueue<PersistantQueueItem> CreateNew()
        {
            return factory.CreateNew();
        }

        public override IPersistantQueue<PersistantQueueItem> CreateNew(string queueName )
        {
            return factory.CreateNew(queueName);
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

	[TestFixture]
    public abstract class CommonTests
    {
        #region Factory methods

        /// <summary>
        /// Abstract method that should be used to call Create(string name) on the concrete tested class
        /// </summary>
        public abstract IPersistantQueue<PersistantQueueItem> Create(String queueName);

        /// <summary>
        /// Abstract method that should be used to call CreateNew() on the concrete tested class
        /// </summary>
        public abstract IPersistantQueue<PersistantQueueItem> CreateNew();

        /// <summary>
        /// Abstract method that should be used to call CreateNew(string name) on the concrete tested class
        /// </summary>
        public abstract IPersistantQueue<PersistantQueueItem> CreateNew(String queueName);

        /// <summary>
        /// Checks factory methods
        /// </summary>
        public abstract void InstanceTypeCheck();

        #endregion

        [Test]
		public void ShouldQueueAndDequeueString()
		{
			using (var queue = this.CreateNew())
			{
				var item = "woot";

				queue.Enqueue(item);

				string dequeued = queue.Dequeue().CastTo<string>();
				dequeued.Should().Be(item);
			}
		}

		[Test]
		public void ShouldQueueAndDequeueInt()
		{
			using (var queue = this.CreateNew())
			{
				var item = 1;

				queue.Enqueue(item);

				var dequeued = queue.Dequeue().CastTo<int>();
				dequeued.Should().Be(item);
			}
		}

		[Test]
		public void ShouldQueueAndPeekString()
		{
			using (var queue = this.CreateNew())
			{
				var item = "woot";

				queue.Enqueue(item);

				var peeked = queue.Peek();
				peeked.Should().Be(item);
			}
		}

		[Test]
		public void ShouldHideInvisibleItemFromPeek()
		{
			using (var queue = this.CreateNew())
			{
				var item = "woot";

				queue.Enqueue(item);
				queue.Dequeue(false, 1000);

				var peeked = queue.Peek().Should().BeNull();
			}
		}

		[Test]
		public void ShouldBeAbleToDequeueAComplexObjectAfterDisposeAndRecreation()
		{
			var queue = this.CreateNew();
			var item = new ComplexObject { SomeTextProperty = "text lololo", SomeInt32Property = 123456 };

			queue.Enqueue(item);

			queue.Dispose();
			using (var newQueue = this.Create(queue.Name))
			{
				var dequeueItem = newQueue.Dequeue();

				dequeueItem.CastTo<ComplexObject>().Should().Equals(item);
			}
		}

		[Test]
		public void ShouldReturnSameQueueIfTheNameIsEqualToAnother()
		{
			using (var queue1 = this.Create("queue"))
			using (var queue2 = this.Create("queue"))
			{
				queue1.Should().BeSameAs(queue2);
			}
		}

		[Test]
		public void ShouldThrownExceptionTryingToCreateNewThatAlreadyExists()
		{
			using (var queue1 = this.Create("queue"))
			{
				Assert.Throws<InvalidOperationException>(() => this.CreateNew("queue"));
			}
		}

		[Test]
		public void ShouldReturnNullWhenQueueIsEmpty()
		{
			using (var queue = this.CreateNew())
			{
				queue.Dequeue().Should().BeNull();
			}
		}

		[Test]
		public void ShouldHideInvisibleMessages()
		{
			using (var queue = this.CreateNew())
			{
				queue.Enqueue("oi");
				queue.Dequeue(false, 1000);

				var item = queue.Dequeue();

				item.Should().BeNull();
			}
		}

		[Test]
		public void ShouldHideInvisibleMessagesUntilTimeout()
		{
			//this test, and any other that depends on the Thread.Sleep, can eventually fail...
			//run it again to be sure it is broken
			using (var queue = this.CreateNew())
			{
				queue.Enqueue("oi");
				queue.Dequeue(false, 1000);

                //Thread.Sleep isn't particularly precise so give this one
                //some breathing room. 1.5 Sec would fail ~50% of the time
				Thread.Sleep(2000);

				var item = queue.Dequeue();
				
				item.Should().NotBeNull();
			}
		}

		[Test]
		public void ShouldRemoveInvisibleItemWhenDeleted()
		{
			using (var queue = this.CreateNew())
			{
				queue.Enqueue("oi");
				var item = queue.Dequeue(false, 100);

				queue.Delete(item);

				Thread.Sleep(1000);

				queue.Dequeue().Should().BeNull();
			}
		}

		[Serializable]
		public class ComplexObject
		{
			public string SomeTextProperty { get; set; }
			public int SomeInt32Property { get; set; }

			public override bool Equals(object obj)
			{
				if (obj is ComplexObject)
				{
					var item = obj as ComplexObject;
					return item.SomeInt32Property == this.SomeInt32Property
						&& item.SomeTextProperty == this.SomeTextProperty;
				}

				return false;
			}
		}
	}
}
