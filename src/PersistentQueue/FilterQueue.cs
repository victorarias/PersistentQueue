using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    public class FilterQueue : Queue<FilterQueueItem>
    {
        public class Factory : PersistentQueueFactory<FilterQueue> { }

        public FilterQueue() : base() { }

        public FilterQueue(string name, bool reset = false)
            : base(name, reset)
		{
            
		}

        /// <summary>
        /// Removes the record from the queue without removing it from the database
        /// </summary>
        public override void Delete(IPersistentQueueItem item)
        {
            this.Delete(item, false);
        }

        /// <summary>
        /// Removes the record from the queue, optionally removing it from the database.
        /// </summary>
        public virtual void Delete(IPersistentQueueItem item, bool removeFromDB)
        {
            if (removeFromDB)
            {
                base.Delete(item);
            }
            else
            {
                var fqItem = (FilterQueueItem)item;
                fqItem.DeleteTime = DateTime.Now;
                store.Update(fqItem);
            }
        }

        /// <summary>
        /// Permanently deletes items where a DeleteTime is set.
        /// </summary>
        public virtual void PurgeDeletedItems()
        {
            lock (store)
            {
                var map = this.store.GetMapping(typeof(FilterQueueItem));
                var query = string.Format("delete from \"{0}\" where \"DeleteTime\" IS NOT NULL", map.TableName);
                this.store.Execute(query);
            }
        }

        public virtual List<FilterQueueItem> AllItems(DateTime? since = null)
        {
            var query = this.store.Table<FilterQueueItem>();

            if (since != null)
            {
                query = query.Where(a => a.CreateTime >= (DateTime)since); 
            }

            return query.ToList();
        }

        public virtual List<FilterQueueItem> ActiveItems(DateTime? since = null)
        {
            var query = this.ActiveItemQuery();

            if (since != null)
            {
                query = query.Where(a => a.CreateTime >= (DateTime)since);
            }

            return query.ToList();
        }

        public virtual List<FilterQueueItem> DeletedItems(DateTime? since = null)
        {
            var query = this.DeletedItemQuery();

            if (since != null)
            {
                query = query.Where(a => a.CreateTime >= (DateTime)since);
            }

            return query.ToList();
        }

        #region Filtering Queries

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

        #endregion
    }

    [Table("FilterQueueItem")]
    public class FilterQueueItem : PersistentQueueItem
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
