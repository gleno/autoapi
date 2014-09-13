using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace zeco.autoapi.Json
{
    public class AutoApiObjectSerializer
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
            var builder = new StringBuilder();

            var batch = _items
                .GroupBy(item => item.TypeIdentity)
                .ToDictionary(g => g.Key, g => g.ToDictionary(item => item.Id.ToString()));

            using (var writer = new JsonTextWriter(new StringWriter(builder)))
                new JsonSerializer {ContractResolver = new JsonContractResolver()}.Serialize(writer, batch);

            return builder.ToString();
        }
    }
}
