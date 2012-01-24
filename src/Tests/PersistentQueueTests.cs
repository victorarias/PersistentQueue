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
			var queue = new Queue();
			var item = "woot";

			queue.Enqueue(item);

			string dequeued = queue.Dequeue<string>();
			dequeued.Should().Be(item);
		}

		[Test]
		public void ShouldQueueAndPeekString()
		{
			var queue = new Queue();
			var item = "woot";

			queue.Enqueue(item);

			var peeked = queue.Peek();
			peeked.Should().Be(item);
		}
	}
}
