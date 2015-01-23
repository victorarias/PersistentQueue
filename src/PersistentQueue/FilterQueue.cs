using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    class FilterQueue : Queue
    {
        protected FilterQueue(string name, bool reset = false)
            : base(name, reset)
		{
            
		}
    }

    public class FilterQueueItem : QueueItem
    {
        [Indexed]
        public DateTime CreateTime { get; set; }

        public DateTime? DeleteTime { get; set; }

        public FilterQueueItem()
        {
            var now = DateTime.Now;
            this.Id = now.Ticks;
            this.CreateTime = now;
            this.DeleteTime = null;
        }
    }
}
