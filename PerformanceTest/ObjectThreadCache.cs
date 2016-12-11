using System;
using System.Collections.Concurrent;

namespace PerformanceTest
{
    public sealed class ObjectPool<T>
    {
        private readonly Func<T> resolve;
        private readonly ConcurrentQueue<T> queue;

        public ObjectPool(Func<T> resolve)
        {
            this.resolve = resolve;
            this.queue = new ConcurrentQueue<T>();
        }

        public T Resolve()
        {
            T instance;
            if (queue.TryDequeue(out instance))
            {
                return instance;
            }
            return resolve();
        }

        public void Release(T instance) => queue.Enqueue(instance);
    }
}