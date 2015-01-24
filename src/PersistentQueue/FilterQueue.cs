using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    class FilterQueue : PersistantQueue<FilterQueueItem>
    {
        protected FilterQueue(string name, bool reset = false)
            : base(name, reset)
		{
            
		}

        /// <summary>
        /// Removes the record from the queue without removing it from the database
        /// </summary>
        public override void Delete(FilterQueueItem item)
        {
            this.Delete(item, false);
        }

        /// <summary>
        /// Removes the record from the queue, optionally removing it from the database.
        /// </summary>
        public virtual void Delete(FilterQueueItem item, bool removeFromDB)
        {
            if (removeFromDB)
            {
                base.Delete(item);
            }
            else
            {
                item.DeleteTime = DateTime.Now;
                store.Update(item);
            }
        }

        protected virtual void PurgeDeletedItems()
        {
            //store.
        }

        protected virtual TableQuery<FilterQueueItem> ActiveItemQuery()
        {
            return store.Table<FilterQueueItem>()
                     .Where(a => null == a.DeleteTime);
        }

        protected virtual TableQuery<FilterQueueItem> DeletedItemQuery()
        {
            return store.Table<FilterQueueItem>()
                     .Where(a => null != a.DeleteTime);
        }

        protected override TableQuery<FilterQueueItem> NextItemQuery()
        {
            return base.NextItemQuery().Where(a => null == a.DeleteTime);
        }
    }

    [Table("FilterQueueItem")]
    public class FilterQueueItem : QueueItem
    {
        [Indexed]
        public DateTime CreateTime { get; set; }

        public DateTime? DeleteTime { get; set; }

        public FilterQueueItem()
        {
            var now = DateTime.Now;
            this.CreateTime = now;
            this.DeleteTime = null;
        }
    }
}
