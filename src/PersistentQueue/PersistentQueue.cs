using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PersistentQueue
{
	public class Queue
	{
		private Queue<object> itens = new Queue<object>();

		public void Enqueue(object obj)
		{
			itens.Enqueue(obj);
		}

		public object Dequeue()
		{
			return itens.Dequeue();
		}

		public T Dequeue<T>()
		{
			return (T)Dequeue();
		}

		public object Peek()
		{
			return itens.Peek();
		}

		public T Peek<T>()
		{
			return (T)Peek();
		}
	}

	public class Item
	{
		public long Id { get; private set; }
		public byte[] Message { get; set; }

		public Item()
		{
			Id = DateTime.Now.Ticks;
		}
	}
}
