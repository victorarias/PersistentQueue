using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace PersistentQueue
{
	public static class Extensions
	{
        public static QueueItemType ToQueueItem<QueueItemType>(this object obj) where QueueItemType : QueueItem , new()
		{
			using(var stream = new MemoryStream()){
				new BinaryFormatter().Serialize(stream, obj);

                return new QueueItemType { Message = stream.ToArray() };
			}
		}

        public static object ToObject<QueueItemType>(this QueueItemType item) where QueueItemType : QueueItem
		{
			using(var stream = new MemoryStream(item.Message))
			{
				return new BinaryFormatter().Deserialize(stream);
			}
		}
	}
}
