using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System.IO;

namespace PersistentQueue
{
	public class Queue : IDisposable
	{
		#region Private Properties

		private const string defaultQueueName = "persistentQueue";
		private SQLite.SQLiteConnection store;
		private bool disposed = false;

		#endregion

		#region Public Properties

		public string Name { get; private set; }

		#endregion

		#region Static

		private static Dictionary<string, Queue> queues = new Dictionary<string, Queue>();

		public static Queue Default
		{
			get
			{
				return Create(defaultQueueName);
			}
		}

		public static Queue CreateNew()
		{
			return CreateNew(defaultQueueName);
		}

		public static Queue CreateNew(string name)
		{
			lock (queues)
			{
				if (queues.ContainsKey(name))
					throw new InvalidOperationException("there is already a queue with that name");

				var queue = new Queue(name, true);
				queues.Add(name, queue);

				return queue;
			}
		}

		public static Queue Create(string name)
		{
			lock (queues)
			{
				Queue queue;

				if (!queues.TryGetValue(name, out queue))
				{
					queue = new Queue(name);
					queues.Add(name, queue);
				}

				return queue;
			}
		}

		#endregion

		private Queue(string name, bool reset = false)
		{
			if (reset && File.Exists(defaultQueueName))
				File.Delete(defaultQueueName);

			Initialize(name);
		}

		~Queue()
		{
			if (!disposed)
			{
				this.Dispose();
			}
		}

		private void Initialize(string name)
		{
			Name = name;
			store = new SQLiteConnection(name);
			store.CreateTable<QueueItem>();
		}

		public void Enqueue(object obj)
		{
			lock (store)
			{
				store.Insert(obj.ToQueueItem());
			}
		}

		public object Dequeue()
		{
			lock (store)
			{
				var item = store.Table<QueueItem>().OrderBy(a => a.Id).First();

				store.Delete(item);

				return item.ToObject();
			}
		}

		public T Dequeue<T>()
		{
			return (T)Dequeue();
		}

		public object Peek()
		{
			lock (store)
			{
				return store.Table<QueueItem>().First().ToObject();
			}
		}

		public T Peek<T>()
		{
			return (T)Peek();
		}

		public void Dispose()
		{
			if (!disposed)
				lock (queues)
				{
					disposed = true;

					queues.Remove(this.Name);
					store.Dispose();

					GC.SuppressFinalize(this);
				}
		}
	}

	public class QueueItem
	{
		[PrimaryKey]
		public long Id { get; private set; }
		public byte[] Message { get; set; }

		public QueueItem()
		{
			Id = DateTime.Now.Ticks;
		}
	}
}
