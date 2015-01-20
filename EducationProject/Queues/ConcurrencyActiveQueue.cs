using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EducationProject.Queues
{
    class ConcurrencyActiveQueue<T> : IDisposable where T : class
    {
        private readonly BlockingCollection<T> blockingCollection;

        public ConcurrencyActiveQueue(int capacity, int workersCount)
        {
            for (int i = 0; i < workersCount; i++)
                ThreadPool.QueueUserWorkItem(ConsumeItems);

            blockingCollection = new BlockingCollection<T>(new ConcurrentQueue<T>(), capacity);
        }

        public void Dispose()
        {
            if (blockingCollection != null)
                blockingCollection.Dispose();
        }

        public event Action<T> ProcessMessage;
        private void FireProcessMessage(T message)
        {
            var handler = ProcessMessage;
            if (handler != null)
                handler(message);
        }

        public void Enqueue(T message)
        {
            blockingCollection.Add(message);
        }

        private void ConsumeItems(object obj)
        {
            foreach (var message in blockingCollection.GetConsumingEnumerable())
                FireProcessMessage(message);
        }
    }
}
