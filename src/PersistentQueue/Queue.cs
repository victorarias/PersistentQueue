using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    public class Queue : PersistantQueue<QueueItem>
    {
        public class Factory : PersistantQueueFactory<Queue> { }

        public Queue() : base() { }

        public Queue(string name, bool reset = false)
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
