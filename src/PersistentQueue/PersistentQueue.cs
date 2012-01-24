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

		public string Name { get; private set; }

		private static Queue @default;
		public static Queue Default
		{
			get
			{
				if (null == @default)
					@default = new Queue();

				return @default;
			}
		}

		public static Queue CreateNew()
		{
			return new Queue(true);
		}

		public static Queue Create(string name)
		{
			return new Queue(name);
		}

		private Queue(string name)
		{
			Initialize(name);
		}

		private Queue()
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

		private Queue(bool reset)
		{
			if (reset && File.Exists(dbName_))
				File.Delete(dbName_);

			Initialize();
		}

		private void Initialize(string name = dbName_)
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
