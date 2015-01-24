using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PersistentQueue
{
    /// <summary>
    /// Abstract class
    /// </summary>
    public class PersistantQueue<QueueItemType> : IDisposable where QueueItemType : PersistantQueueItem, new()
	{
		#region Private Properties

		protected const string defaultQueueName = "persistentQueue";
        protected SQLite.SQLiteConnection store;
        protected bool disposed = false;

		#endregion

		#region Public Properties

        public string Name { get; protected set; }

		#endregion

		#region Static

        private static Dictionary<string, PersistantQueue<QueueItemType>> queues = new Dictionary<string, PersistantQueue<QueueItemType>>();

        public static PersistantQueue<QueueItemType> Default
		{
			get
			{
				return Create(defaultQueueName);
			}
		}

        public static PersistantQueue<QueueItemType> CreateNew()
		{
			return CreateNew(defaultQueueName);
		}

        public static PersistantQueue<QueueItemType> CreateNew(string name)
		{
			lock (queues)
			{
				if (queues.ContainsKey(name))
					throw new InvalidOperationException("there is already a queue with that name");
                
				var queue = new PersistantQueue<QueueItemType>(name, true);
				queues.Add(name, queue);

				return queue;
			}
		}

        public static PersistantQueue<QueueItemType> Create(string name)
		{
			lock (queues)
			{
                PersistantQueue<QueueItemType> queue;

				if (!queues.TryGetValue(name, out queue))
				{
					queue = new PersistantQueue<QueueItemType>(name);
					queues.Add(name, queue);
				}

				return queue;
			}
		}

		#endregion

        protected PersistantQueue(string name, bool reset = false)
		{
			if (reset && File.Exists(defaultQueueName))
				File.Delete(defaultQueueName);

			Initialize(name);
		}

        ~PersistantQueue()

		{
			if (!disposed)
			{
				this.Dispose();
			}
		}

        protected void Initialize(string name)
		{
			Name = name;
			store = new SQLiteConnection(name);
            store.CreateTable<QueueItemType>();
		}

		public void Enqueue(object obj)
		{
			lock (store)
			{
                store.Insert(obj.ToQueueItem<QueueItemType>());
			}
		}

        public QueueItemType Dequeue(bool remove = true, int invisibleTimeout = 30000)
		{
			lock (store)
			{
				var item = GetNextItem();

				if (null != item)
				{
                    if (remove)
                    {
                        this.Delete(item);
                    }
                    else
                    {
                        this.Invalidate(item, invisibleTimeout);
                    }

					return item;
				}
				else
				{
					return default(QueueItemType);
				}
			}
		}

        public virtual void Invalidate(QueueItemType item, int invisibleTimeout = 30000)
        {
            item.InvisibleUntil = DateTime.Now.AddMilliseconds(invisibleTimeout);
            store.Update(item);
        }

        public virtual void Delete(QueueItemType item)
		{
			store.Delete(item);
		}

		public object Peek()
		{
			lock (store)
			{
				var item = GetNextItem();
				
				return null == item ? null : item.ToObject();
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

        protected QueueItemType GetNextItem()
		{
            return this.NextItemQuery().FirstOrDefault();
		}

        protected virtual TableQuery<QueueItemType> NextItemQuery()
        {
            return store.Table<QueueItemType>()
                     .Where(a => DateTime.Now > a.InvisibleUntil)
                     .OrderBy(a => a.Id);
        }
	}

    [Table("PersistantQueueItem")]
    public abstract class PersistantQueueItem
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; protected set; }

        [Indexed]
        public DateTime InvisibleUntil { get; set; }

        public byte[] Message { get; set; }

        public PersistantQueueItem() { }

        public T CastTo<T>()
        {
            return (T)this.ToObject();
        }
    }

    public static class Extensions
    {
        public static QueueItemType ToQueueItem<QueueItemType>(this object obj) where QueueItemType : PersistantQueueItem, new()
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, obj);

                return new QueueItemType { Message = stream.ToArray() };
            }
        }

        public static object ToObject<QueueItemType>(this QueueItemType item) where QueueItemType : PersistantQueueItem
        {
            using (var stream = new MemoryStream(item.Message))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
