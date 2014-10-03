using System;
using System.Threading;

namespace zeco.autoapi.Providers
{
    public abstract class ThreadLocalProvider<T>
    {
        private readonly ThreadLocal<T> _local;
        private readonly object _lock = new object();

        public T LocalInstance
        {
            get { return _local.Value; }
        }

        protected ThreadLocalProvider()
        {
            _local = new ThreadLocal<T>(CreateInstanceInternal);
        }

        private T CreateInstanceInternal()
        {
            lock (_lock)
            {
                return CreateInstance();
            }
        }

        protected virtual T CreateInstance()
        {
            return Activator.CreateInstance<T>();
        }
    }
}