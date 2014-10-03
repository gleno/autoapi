using System;

namespace zeco.autoapi.Providers
{
    public class ThreadLocalRandomProvider : ThreadLocalProvider<Random>
    {
        private static readonly ThreadLocalRandomProvider _provider = new ThreadLocalRandomProvider();

        public static Random Instance
        {
            get { return _provider.LocalInstance; }
        }

        private readonly Random _random = new Random();

        protected override Random CreateInstance()
        {
            return new Random(_random.Next());
        }
    }
}