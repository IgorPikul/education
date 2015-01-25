using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EducationProject.Queues
{
    class ActiveQueue<T> : IDisposable where T : class
    {
        private readonly Queue<T> queue;
        private readonly object syncObject = new object();
        private readonly int capacity;

        private readonly Thread[] threads;
        private ManualResetEvent terminateEvent = new ManualResetEvent(false);

        public ActiveQueue(int capacity, int workersCount)
        {
            this.capacity = capacity;
            queue = new Queue<T>(capacity);

            threads = new Thread[workersCount];
            for (int i = 0; i < workersCount; i++)
            {
                threads[i] = new Thread(ThreadFn);
                threads[i].IsBackground = true;
                threads[i].Start();
            }
        }

        public void Dispose()
        {
            terminateEvent.Set();
            lock (syncObject)
                Monitor.PulseAll(syncObject);
        }
       
        public event Action<T> ProcessMessage;
        private void FireProcessMessage(T message)
        {
            var handler = ProcessMessage;
            if (handler != null)
                handler(message);
        }

        public void Enqueue(T item)
        {
            lock (syncObject)
            {
                while (queue.Count == capacity)
                    Monitor.Wait(syncObject);
                if (queue.Count == 0)
                    Monitor.Pulse(syncObject);

                queue.Enqueue(item);
            }
        }

   
        private void ThreadFn(object obj)
        {
            while (true)
            {
                if (terminateEvent.WaitOne(0))
                    return;

                T item = default(T);
                lock (syncObject)
                {
                    while (queue.Count == 0)
                    {
                        Monitor.Wait(syncObject);
                        if (terminateEvent.WaitOne(0))
                            return;
                    }

                    if (queue.Count == capacity)
                        Monitor.Pulse(syncObject);

                    item = queue.Dequeue();
                }
                FireProcessMessage(item);
            }
        }
    }
}
