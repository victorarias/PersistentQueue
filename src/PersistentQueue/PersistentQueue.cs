using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PersistentQueue
{
    public interface IPersistentQueueItem
    {
        long Id { get; }
        DateTime InvisibleUntil { get; set; }
        byte[] Message { get; set; }
        T CastTo<T>();
        String TableName();
    }


    /// <summary>
    /// General Persistent Queue interface
    /// </summary>
    public interface IPersistentQueue : IDisposable
    {
        #region Public Properties

        string Name { get; }

        #endregion

        void Enqueue(object obj);
        IPersistentQueueItem Dequeue(bool remove = true, int invisibleTimeout = 30000);
        void Invalidate(IPersistentQueueItem item, int invisibleTimeout = 30000);
        void Delete(IPersistentQueueItem item);
        object Peek();
        T Peek<T>();
        String TableName();
    }

    /// <summary>
    /// Represents a factory that builds specific PersistentQueue implementations
    /// </summary>
    public interface IPersistentQueueFactory
    {
        /// <summary>
        /// Creates or returns a PersistentQueue instance with default parameters for storage.
        /// </summary>
        IPersistentQueue Default();

        /// <summary>
        /// Creates or returns a PersistentQueue instance that is stored at given path.
        /// </summary>
        IPersistentQueue Create(string name);

        /// <summary>
        /// Attempts to create a new PersistentQueue instance with default parameters for storage.
        /// If the instance was already loaded, an exception will be thrown.
        /// </summary>
        IPersistentQueue CreateNew();

        /// <summary>
        /// Attempts to create a new PersistentQueue instance that is stored at the given path.
        /// If the instance was already loaded, an exception will be thrown.
        /// </summary>
        IPersistentQueue CreateNew(string name);
    }

    public class QueueStorageMismatchException : Exception
    {
        public QueueStorageMismatchException(String message) : base(message) { }

        public QueueStorageMismatchException(IPersistentQueue queue, IPersistentQueueItem invalidQueueItem)
            : base(BuildMessage(queue, invalidQueueItem))
        {

        }

        private static String BuildMessage(IPersistentQueue queue, IPersistentQueueItem invalidQueueItem)
        {
            return String.Format("Queue Item of type {0} stores data to a table named \"{1}\". Queue of type {2} stores data to a table names \"{3}\"",
                                 invalidQueueItem.GetType(),
                                 invalidQueueItem.TableName(),
                                 queue.GetType(),
                                 queue.TableName());
        }
    }

    /// <summary>
    /// A class that implements a Persistent SQLite backed queue
    /// </summary>
    public abstract class Queue<QueueItemType> : IPersistentQueue where QueueItemType : PersistentQueueItem, new()
	{
        #region Factory

        private static Dictionary<string, IPersistentQueue> queues = new Dictionary<string, IPersistentQueue>();

        public abstract class PersistentQueueFactory<ConcreteType> : IPersistentQueueFactory where ConcreteType : Queue<QueueItemType>, new()
        {

            public IPersistentQueue Default()
            {
                return Create(defaultQueueName);
            }

            public IPersistentQueue Create(string name)
            {
                lock (queues)
                {
                    IPersistentQueue queue;

                    if (!queues.TryGetValue(name, out queue))
                    {
                        queue = new ConcreteType();
                        ((ConcreteType)queue).Initialize(name);
                        queues.Add(name, queue);
                    }

                    return queue;
                }
            }

            public IPersistentQueue CreateNew()
            {
                return CreateNew(defaultQueueName);
            }

            public IPersistentQueue CreateNew(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name",
                        "CreateNew(string name) requires a non null name parameter. Consider calling CreateNew() instead if you do not want to pass in a name");
                }

                lock (queues)
                {
                    if (queues.ContainsKey(name))
                    {
                        throw new InvalidOperationException("there is already a queue with that name");
                    }

                    ConcreteType queue = new ConcreteType();
                    queue.Initialize(name, true);
                    queues.Add(name, queue);

                    return (IPersistentQueue)queue;
                }
            }
        }

		#endregion

        #region Private Properties

        protected const string defaultQueueName = "persistentQueue";
        protected SQLite.SQLiteConnection store;
        protected bool disposed = false;

        #endregion

        #region Public Properties

        public string Name { get; protected set; }

        #endregion

        public Queue()
        {

        }

        public Queue(string name, bool reset = false)
		{
            Initialize(name, reset);
		}

        ~Queue()
		{
			if (!disposed)
			{
				this.Dispose();
			}
		}

        protected virtual void Initialize(string name, bool reset = false)
		{
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (reset && File.Exists(defaultQueueName))
            {
                File.Delete(defaultQueueName);
            }

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

        public IPersistentQueueItem Dequeue(bool remove = true, int invisibleTimeout = 30000)
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

        public virtual void Invalidate(IPersistentQueueItem item, int invisibleTimeout = 30000)
        {
            if (item is QueueItemType)
            {
                item.InvisibleUntil = DateTime.Now.AddMilliseconds(invisibleTimeout);
                store.Update(item);
            }
            else
            {
                throw new QueueStorageMismatchException(this, item);
            }
        }

        public virtual void Delete(IPersistentQueueItem item)
		{
            if (item is QueueItemType)
            {
                store.Delete(item);
            }
            else
            {
                throw new QueueStorageMismatchException(this, item);
            }
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
			if (!disposed) {
                lock (queues)
                {
                    disposed = true;

					queues.Remove(this.Name);
					store.Dispose();

					GC.SuppressFinalize(this);
				}
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

        public String TableName()
        {
            String name = null;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(typeof(QueueItemType));

            foreach (System.Attribute attr in attrs)
            {
                if (attr is SQLite.TableAttribute)
                {
                    var a = (SQLite.TableAttribute)attr;
                    name = a.Name;
                    break;
                }
            }

            return name;
        }
	}

    [Table("PersistentQueueItem")]
    public abstract class PersistentQueueItem : IPersistentQueueItem
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; protected set; }

        [Indexed]
        public DateTime InvisibleUntil { get; set; }

        public byte[] Message { get; set; }

        public PersistentQueueItem() { }

        public T CastTo<T>()
        {
            return (T)this.ToObject();
        }

        public String TableName()
        {
            String name = null;

            System.Attribute[] attrs = System.Attribute.GetCustomAttributes(this.GetType());

            foreach (System.Attribute attr in attrs)
            {
                if (attr is SQLite.TableAttribute)
                {
                    var a = (SQLite.TableAttribute)attr;
                    name = a.Name;
                    break;
                }
            }

            return name;
        }
    }

    public static class Extensions
    {
        public static QueueItemType ToQueueItem<QueueItemType>(this object obj) where QueueItemType : PersistentQueueItem, new()
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, obj);

                return new QueueItemType { Message = stream.ToArray() };
            }
        }

        public static object ToObject<QueueItemType>(this QueueItemType item) where QueueItemType : PersistentQueueItem
        {
            using (var stream = new MemoryStream(item.Message))
            {
                return new BinaryFormatter().Deserialize(stream);
            }
        }
    }
}
