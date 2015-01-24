using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    public class Queue : PersistantQueue<QueueItem>
    {
        protected Queue(string name, bool reset = false)
            : base(name, reset)
		{
            
		}
    }

    [Table("QueueItem")]
    public class QueueItem : PersistantQueueItem
    {        
        public QueueItem()
        {
            Id = DateTime.Now.Ticks;
        }
    }
}
