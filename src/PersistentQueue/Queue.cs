using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;

namespace PersistentQueue
{
    public class Queue : Queue<QueueItem>
    {
        public class Factory : PersistentQueueFactory<Queue> { }

        public Queue() : base() { }

        public Queue(string name, bool reset = false)
            : base(name, reset)
		{
            
		}
    }

    [Table("QueueItem")]
    public class QueueItem : PersistentQueueItem
    {        
        public QueueItem()
        {
            Id = DateTime.Now.Ticks;
        }
    }
}
