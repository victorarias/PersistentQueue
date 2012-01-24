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
		public static QueueItem ToQueueItem(this object obj)
		{
			using(var stream = new MemoryStream()){
				new BinaryFormatter().Serialize(stream, obj);
				
				return new QueueItem { Message = stream.ToArray() };
			}
		}

		public static object ToObject(this QueueItem item)
		{
			using(var stream = new MemoryStream(item.Message))
			{
				return new BinaryFormatter().Deserialize(stream);
			}
		}
	}
}
