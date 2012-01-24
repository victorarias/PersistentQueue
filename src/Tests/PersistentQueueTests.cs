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
			using (var queue = new Queue(true))
			{
				var item = "woot";

				queue.Enqueue(item);

				string dequeued = queue.Dequeue<string>();
				dequeued.Should().Be(item);
			}
		}

		[Test]
		public void ShouldQueueAndPeekString()
		{
			using (var queue = new Queue(true))
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
			var queue = new Queue(true);
			var item = new ComplexObject { SomeTextProperty = "text lololo", SomeInt32Property = 123456 };

			queue.Enqueue(item);

			queue.Dispose(); queue = null;
			using (var newQueue = new Queue())
			{
				var dequeueItem = newQueue.Dequeue();

				dequeueItem.Should().Equals(item);
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
