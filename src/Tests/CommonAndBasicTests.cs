using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersistentQueue;
using FluentAssertions;
using System.Threading;
using SQLite;
using ZipRecruiter;

namespace Tests
{
    /// <summary>
    /// Performs type checking tests
    /// </summary>
    [TestFixture]
    public class BasicTests
    {
        [TestFixtureSetUp]
        public void Init()
        {
            SQLiteShim.InjectSQLite();
        }

        /// <summary>
        /// Ensures that IPersistentQueue and IPersistentQueueFactory types
        /// can be assigned to and used interchangeably
        /// </summary>
        [Test]
        public void InterfaceCovarience()
        {
            IPersistentQueue queue;
            IPersistentQueueFactory factory;

            factory = new Queue.Factory();
            using (queue = factory.CreateNew()) { }

            factory = new FilterQueue.Factory();
            using (queue = factory.CreateNew()) { };
        }
    }

    /// <summary>
    /// An abstract class containing tests that should apply to all PersistentQueue subclasses
    /// </summary>
	[TestFixture]
    public abstract class CommonTests
    {
        [TestFixtureSetUp]
        public void Init()
        {
            SQLiteShim.InjectSQLite();
        }

        #region Factory methods

        /// <summary>
        /// A Factory that builds the test class
        /// </summary>
        protected IPersistentQueueFactory factory;

        /// <summary>
        /// Abstract method used to build the tested Class' factory
        /// </summary>
        public abstract IPersistentQueueFactory BuildFactory();

        /// <summary>
        /// Shortcut to call Create(string name) on the concrete test class' factory
        /// </summary>
        public virtual IPersistentQueue Create(string queueName)
        {
            return factory.Create(queueName);
        }

        /// <summary>
        /// Shortcut to call CreateNew() on the concrete test class' factory
        /// </summary>
        public virtual IPersistentQueue CreateNew()
        {
            return factory.CreateNew();
        }

        /// <summary>
        /// Shortcut to call CreateNew(string name) on the concrete test class' factory
        /// </summary>
        public virtual IPersistentQueue CreateNew(string queueName)
        {
            return factory.CreateNew(queueName);
        }

        /// <summary>
        /// Performs type checks on the Interfaces returned by factory methods
        /// </summary>
        public abstract void InstanceTypeCheck();

        public CommonTests()
        {
            factory = BuildFactory();
        }

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
