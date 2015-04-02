using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PersistentQueue
{
    public interface IPersistantQueueItem
    {
        long Id { get; }
        DateTime InvisibleUntil { get; set; }
        byte[] Message { get; set; }
        T CastTo<T>();
        String TableName();
    }


    /// <summary>
    /// Abstract class
    /// </summary>
    public interface IPersistantQueue : IDisposable
    {
        #region Public Properties

        string Name { get; }

        #endregion

        void Enqueue(object obj);
        IPersistantQueueItem Dequeue(bool remove = true, int invisibleTimeout = 30000);
        void Invalidate(IPersistantQueueItem item, int invisibleTimeout = 30000);
        void Delete(IPersistantQueueItem item);
        object Peek();
        T Peek<T>();
        String TableName();
    }

    /// <summary>
    /// Represents a factory that builds specific PersistantQueue implementations
    /// </summary>
    public interface IPersistantQueueFactory
    {
        /// <summary>
        /// Creates or returns a PersistantQueue instance with default parameters for storage.
        /// </summary>
        IPersistantQueue Default();

        /// <summary>
        /// Creates or returns a PersistantQueue instance that is stored at given path.
        /// </summary>
        IPersistantQueue Create(string name);

        /// <summary>
        /// Attempts to create a new PersistantQueue instance with default parameters for storage.
        /// If the instance was already loaded, an exception will be thrown.
        /// </summary>
        IPersistantQueue CreateNew();

        /// <summary>
        /// Attempts to create a new PersistantQueue instance that is stored at the given path.
        /// If the instance was already loaded, an exception will be thrown.
        /// </summary>
        IPersistantQueue CreateNew(string name);
    }

    public class QueueStorageMismatchException : Exception
    {
        public QueueStorageMismatchException(String message) : base(message) { }

        public QueueStorageMismatchException(IPersistantQueue queue, IPersistantQueueItem invalidQueueItem)
            : base(BuildMessage(queue, invalidQueueItem))
        {

        }

        private static String BuildMessage(IPersistantQueue queue, IPersistantQueueItem invalidQueueItem)
        {
            return String.Format("Queue Item of type {0} stores data to a table named \"{1}\". Queue of type {2} stores data to a table names \"{3}\"",
                                 invalidQueueItem.GetType(),
                                 invalidQueueItem.TableName(),
                                 queue.GetType(),
                                 queue.TableName());
        }
    }

    public static class PersistantQueueStorage
    {
        private static Dictionary<string, IPersistantQueue> queues = new Dictionary<string, IPersistantQueue>();
        public static Dictionary<string, IPersistantQueue> Queues
        {
            get
            {
                return queues;
            }
        }
    }

    /// <summary>
    /// A class that implements a persistant SQLite backed queue
    /// </summary>
    public abstract class Queue<QueueItemType> : IPersistantQueue where QueueItemType : PersistantQueueItem, new()
	{
        #region Factory

        private static Dictionary<string, IPersistantQueue> queues = PersistantQueueStorage.Queues;

        public abstract class PersistantQueueFactory<ConcreteType> : IPersistantQueueFactory where ConcreteType : Queue<QueueItemType>, new()
        {

            public IPersistantQueue Default()
            {
                return Create(defaultQueueName);
            }

            public IPersistantQueue Create(string name)
            {
                lock (queues)
                {
                    IPersistantQueue queue;

                    if (!queues.TryGetValue(name, out queue))
                    {
                        queue = new ConcreteType();
                        ((ConcreteType)queue).Initialize(name);
                        queues.Add(name, queue);
                    }

                    return queue;
                }
            }

            public IPersistantQueue CreateNew()
            {
                return CreateNew(defaultQueueName);
            }

            public IPersistantQueue CreateNew(string name)
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

                    return (IPersistantQueue)queue;
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

        public IPersistantQueueItem Dequeue(bool remove = true, int invisibleTimeout = 30000)
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

        public virtual void Invalidate(IPersistantQueueItem item, int invisibleTimeout = 30000)
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

        public virtual void Delete(IPersistantQueueItem item)
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

    [Table("PersistantQueueItem")]
    public abstract class PersistantQueueItem : IPersistantQueueItem
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
