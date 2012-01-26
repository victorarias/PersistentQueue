using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PersistentQueue;
using FluentAssertions;

namespace Tests
{
	[TestFixture]
	public class PersistentQueueTests
	{
		[Test]
		public void ShouldQueueAndDequeueString()
		{
			using (var queue = Queue.CreateNew())
			{
				var item = "woot";

				queue.Enqueue(item);

				string dequeued = queue.Dequeue<string>();
				dequeued.Should().Be(item);
			}
		}

		[Test]
		public void ShouldQueueAndDequeueInt()
		{
			using (var queue = Queue.CreateNew())
			{
				var item = 1;

				queue.Enqueue(item);

				var dequeued = queue.Dequeue<int>();
				dequeued.Should().Be(item);
			}
		}

		[Test]
		public void ShouldQueueAndPeekString()
		{
			using (var queue = Queue.CreateNew())
			{
				var item = "woot";

				queue.Enqueue(item);

				var peeked = queue.Peek();
				peeked.Should().Be(item);
			}
		}

		[Test]
		public void ShouldBeAbleToDequeueAComplexObjectAfterDisposeAndRecreation()
		{
			var queue = Queue.CreateNew();
			var item = new ComplexObject { SomeTextProperty = "text lololo", SomeInt32Property = 123456 };

			queue.Enqueue(item);

			queue.Dispose();
			using (var newQueue = Queue.Create(queue.Name))
			{
				var dequeueItem = newQueue.Dequeue();

				dequeueItem.Should().Equals(item);
			}
		}

		[Test]
		public void ShouldReturnSameQueueIfTheNameIsEqualToAnother()
		{
			var queue1 = Queue.Create("queue");
			var queue2 = Queue.Create("queue");

			queue1.Should().BeSameAs(queue2);
		}

		[Test]
		public void ShouldThrownExceptionTryingToCreateNewThatAlreadyExists()
		{
			var queue1 = Queue.Create("queue");
			
			Assert.Throws<InvalidOperationException>(() => Queue.CreateNew("queue"));
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
