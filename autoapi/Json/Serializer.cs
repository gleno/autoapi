using System.Collections;
using System.Collections.Generic;
using System.Linq;
using autoapi.Extensions;

namespace autoapi.Json
{
    public class AutoApiEntityCollection : IEnumerable<IIdentifiable>
    {
        private readonly List<IIdentifiable> _items = new List<IIdentifiable>();

        public void Add(IIdentifiable item)
        {
            _items.Add(item);
        }

        public void Add(IEnumerable<IIdentifiable> items)
        {
            foreach (var item in items) Add(item);
        }

        public string Serialize()
        {
            return _items
                .GroupBy(item => item.TypeIdentity)
                .ToDictionary(g => g.Key, g => g.ToDictionary(item => item.Id.ToString()))
                .ToSafeJson();
        }

        public IEnumerator<IIdentifiable> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
