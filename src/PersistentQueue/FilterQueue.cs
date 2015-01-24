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

        public virtual void Delete(FilterQueueItem item, bool removeFromDB = false)
        {
            if (removeFromDB)
            {
                store.Delete(item);
            }
            else
            {
                item.DeleteTime = DateTime.Now;
                store.Update(item);
            }
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
            //this.Id = now.Ticks;
            this.CreateTime = now;
            this.DeleteTime = null;
        }
    }
}
