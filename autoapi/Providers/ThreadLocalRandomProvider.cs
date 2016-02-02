using System;

namespace autoapi.Providers
{
    public class ThreadLocalRandomProvider : ThreadLocalProvider<Random>
    {
        private static readonly ThreadLocalRandomProvider Provider = new ThreadLocalRandomProvider();

        public static Random Instance => Provider.LocalInstance;

        private readonly Random _random = new Random();

        protected override Random CreateInstance()
        {
            return new Random(_random.Next());
        }
    }
}