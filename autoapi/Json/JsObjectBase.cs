using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace zeco.autoapi.Json
{
    public class JsObjectBase : DynamicObject, IDictionary<string, object>
    {
        #region Fields

        protected readonly IDictionary<string, object> Properties
            = new Dictionary<string, object>();

        #endregion

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return Properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Properties.GetEnumerator();
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            Properties.Add(item);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            Properties.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return Properties.Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            Properties.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return Properties.Remove(item);
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return Properties.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return Properties.IsReadOnly; }
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            Properties.Add(key, value);
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            return Properties.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            return Properties.TryGetValue(key, out value);
        }

        object IDictionary<string, object>.this[string key]
        {
            get { return Properties[key]; }
            set { Properties[key] = value; }
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return Properties.Keys; }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return Properties.Values; }
        }

    }
}