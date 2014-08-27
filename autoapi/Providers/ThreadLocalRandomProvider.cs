using System;
using System.Threading;

namespace zeco.autoapi.Providers
{
    public static class ThreadLocalRandomProvider
    {
        /// <summary> 
        /// Random number generator used to generate seeds, 
        /// which are then used to create new random number 
        /// generators on a per-thread basis. 
        /// </summary> 
        private static readonly Random _globalRandom = new Random();
        private static readonly object _globalLock = new object();

        /// <summary> 
        /// Random number generator 
        /// </summary> 
        private static readonly ThreadLocal<Random> _threadRandom = new ThreadLocal<Random>(NewRandom);

        /// <summary> 
        /// Creates a new instance of Random. The seed is derived 
        /// from a global (static) instance of Random, rather 
        /// than time. 
        /// </summary> 
        public static Random NewRandom()
        {
            lock (_globalLock)
            {
                return new Random(_globalRandom.Next());
            }
        }

        /// <summary> 
        /// Returns an instance of Random which can be used freely 
        /// within the current thread. 
        /// </summary> 
        public static Random Instance { get { return _threadRandom.Value; } }

        /// <summary>See <see cref="Random.Next()" /></summary> 
        public static int Next()
        {
            return Instance.Next();
        }

        /// <summary>See <see cref="Random.Next(int)" /></summary> 
        public static int Next(int maxValue)
        {
            return Instance.Next(maxValue);
        }

        /// <summary>See <see cref="Random.Next(int, int)" /></summary> 
        public static int Next(int minValue, int maxValue)
        {
            return Instance.Next(minValue, maxValue);
        }

        /// <summary>See <see cref="Random.NextDouble()" /></summary> 
        public static double NextDouble()
        {
            return Instance.NextDouble();
        }

        /// <summary>See <see cref="Random.NextBytes(byte[])" /></summary> 
        public static void NextBytes(byte[] buffer)
        {
            Instance.NextBytes(buffer);
        }
    }


}