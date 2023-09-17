using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.utils
{
    public class ScopedDictionary<K, V> where K: notnull
    {
        public class Scope : IDisposable
        {
            internal readonly ScopedDictionary<K, V> dict;
            internal readonly int level;
            internal bool disposed = false;
            internal Scope(ScopedDictionary<K, V> dict, int level) { this.dict = dict; this.level = level; }

            public void Dispose() {
                if (disposed) return;
                if (dict._list.Count != level + 1)
                    throw new Exception("Error: inconsistent scopes");
                dict._list.RemoveAt(level);
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private List<Dictionary<K, V>> _list;

        public ScopedDictionary()
        {
            _list = new()
            {
                new Dictionary<K, V>()
            };
        }

        public Scope EnterNewScope()
        {
            _list.Add(new Dictionary<K, V>());
            return new Scope(this, _list.Count - 1);
        }

        public Dictionary<K, V> GlobalScope => _list[0];

        public void Add(K key, V value)
        {
            _list.Last()[key] = value;
        }

        public V Get(K key)
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (_list[i].TryGetValue(key, out V? v))
                    return v!;
            }
            throw new Exception("Symbol not found");
        }

        public (V, int) GetAndScope(K key)
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (_list[i].TryGetValue(key, out V? v))
                    return (v!, i);
            }
            throw new Exception("Symbol not found");
        }

        public bool Contains(K key)
        {
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                if (_list[i].ContainsKey(key)) return true;
            }
            return false;
        }

        public void Reset()
        {
            _list = new List<Dictionary<K, V>>()
            {
                new Dictionary<K, V>()
            };
        }

        public V this[K key] => Get(key);
    }
}
