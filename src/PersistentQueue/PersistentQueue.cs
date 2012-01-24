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
		private const string dbName_ = "persistentQueue";
		private SQLite.SQLiteConnection store;
		private bool disposed = false;

		public Queue()
		{
			Initialize();
		}

		~Queue()
		{
			if (!disposed)
			{
				disposed = true;
				store.Dispose();
			}
		}

		public Queue(bool reset)
		{
			if (reset && File.Exists(dbName_))
				File.Delete(dbName_);

			Initialize();
		}

		private void Initialize()
		{
			store = new SQLiteConnection(dbName_);
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
			{
				disposed = true;
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
