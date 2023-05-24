using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace ChainFx
{
    /// <summary>
    /// An add-only data collection that can act as both a list, a dictionary and/or a two-layered tree.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class Map<K, V> : IEnumerable<Map<K, V>.Entry>
        where K : IEquatable<K>, IComparable<K>
    {
        int[] buckets;

        protected Entry[] entries;

        int count;

        // current group head
        int head = -1;

        public Map(int capacity = 16)
        {
            // find a least power of 2 that is greater than or equal to capacity
            int size = 8;
            while (size < capacity)
            {
                size <<= 1;
            }

            ReInit(size);
        }

        private void ReInit(int size) // must be power of 2
        {
            // allocalte new arrays as needed
            if (entries == null || size > entries.Length)
            {
                buckets = new int[size];
                entries = new Entry[size];
            }

            // initialize all buckets to -1
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = -1;
            }

            count = 0;
        }

        /// <summary>
        /// Gets the number of added entries.
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Gets the entry at the specified ordinal position.
        /// </summary>
        /// <param name="idx">0-based position</param>
        /// <returns></returns>
        public Entry EntryAt(int idx) => entries[idx];

        /// <summary>
        /// Finds the entry with the specified key.
        /// </summary>
        /// <param name="key">A key to find</param>
        /// <returns></returns>
        public Entry EntryOf(K key)
        {
            var idx = IndexOf(key);
            if (idx > -1)
            {
                return entries[idx];
            }
            return default;
        }

        public K KeyAt(int idx) => entries[idx].key;

        public V ValueAt(int idx) => entries[idx].value;

        public V[] SubGroupOf(K key)
        {
            var idx = IndexOf(key);
            if (idx > -1)
            {
                int tail = entries[idx].tail;
                int ret = tail - idx; // number of returned elements
                var arr = new V[ret];
                for (int i = 0; i < ret; i++)
                {
                    arr[i] = entries[idx + 1 + i].value;
                }
                return arr;
            }
            return null;
        }

        public int IndexOf(K key)
        {
            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % buckets.Length; // target bucket
            int idx = buckets[buck];
            while (idx != -1)
            {
                var e = entries[idx];
                if (e.Match(code, key))
                {
                    return idx;
                }

                idx = entries[idx].next; // adjust for next index
            }

            return -1;
        }

        public void Clear()
        {
            if (entries != null)
            {
                ReInit(entries.Length);
            }
        }

        public void Add(K key, V value)
        {
            Add(key, value, false);
        }

        public void Add<T>(T v) where T : V, IKeyable<K>
        {
            Add(v.Key, v, false);
        }

        private void Add(K key, V value, bool rehash)
        {
            // ensure double-than-needed capacity
            if (!rehash && count >= entries.Length / 2)
            {
                var oldEntires = entries;
                int oldCount = count;
                ReInit(entries.Length * 2);

                // re-add old elements
                for (int i = 0; i < oldCount; i++)
                {
                    Add(oldEntires[i].key, oldEntires[i].value, true);
                }
            }

            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % buckets.Length; // target bucket
            int idx = buckets[buck];
            while (idx != -1)
            {
                if (entries[idx].Match(code, key))
                {
                    entries[idx].value = value;
                    return; // replace the old value
                }

                idx = entries[idx].next; // adjust for next index
            }

            // add a new entry
            idx = count;
            entries[idx] = new Entry(code, buckets[buck], key, value);
            buckets[buck] = idx;
            count++;

            // decide group
            if (value is IGroupable<K> grpable)
            {
                // compare to current head
                if (head == -1 || !grpable.GroupWith(entries[head].key))
                {
                    head = idx;
                }

                entries[head].tail = idx;
            }
        }

        /// <summary>
        /// Determines whether the Map&lt;K,V&gt; contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the Map&lt;K,V&gt;.</param>
        /// <returns>true if the Map&lt;K,V&gt; contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(K key)
        {
            if (TryGetValue(key, out _))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        public V this[K key] => TryGetValue(key, out var v) ? v : default;


        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the map contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(K key, out V value)
        {
            if (key != null)
            {
                int code = key.GetHashCode() & 0x7fffffff;
                int buck = code % buckets.Length; // target bucket
                int idx = buckets[buck];
                while (idx != -1)
                {
                    var e = entries[idx];
                    if (e.Match(code, key))
                    {
                        value = e.value;
                        return true;
                    }

                    idx = entries[idx].next; // adjust for next index
                }
            }
            value = default;
            return false;
        }

        public bool TryResetValue(K key, out V value)
        {
            if (key != null)
            {
                int code = key.GetHashCode() & 0x7fffffff;
                int buck = code % buckets.Length; // target bucket
                int idx = buckets[buck];
                while (idx != -1)
                {
                    var e = entries[idx];
                    if (e.Match(code, key))
                    {
                        value = e.value;
                        e.value = default; // reset value

                        return true;
                    }

                    idx = entries[idx].next; // adjust for next index
                }
            }

            value = default;
            return false;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return new Enumerator(this);
        }


        public V[] All(Predicate<V> cond = null)
        {
            var lst = new ValueList<V>(16);
            for (int i = 0; i < count; i++)
            {
                var value = entries[i].value;
                if (value == null) // been set to null
                {
                    continue;
                }
                if (cond == null || cond(value))
                {
                    lst.Add(value);
                }
            }
            return lst.ToArray();
        }

        public Map<R, V> All<R>(Func<K, V, bool> cond, Func<V, R> keyer) where R : IEquatable<R>, IComparable<R>
        {
            var map = new Map<R, V>(32);
            for (int i = 0; i < count; i++)
            {
                K key = entries[i].key;
                V value = entries[i].value;
                if (value == null) // been set to null
                {
                    continue;
                }
                if (cond(key, value))
                {
                    map.Add(keyer(value), value);
                }
            }
            return map;
        }

        public V First(Predicate<V> filter)
        {
            for (int i = 0; i < count; i++)
            {
                var value = entries[i].value;
                if (value == null) // been set to null
                {
                    continue;
                }
                if (filter == null || filter(value))
                {
                    return value;
                }
            }
            return default;
        }

        public void ForEach(Func<K, V, bool> cond, Action<K, V> handler)
        {
            for (int i = 0; i < count; i++)
            {
                K key = entries[i].key;
                V value = entries[i].value;
                if (cond == null || cond(key, value))
                {
                    handler(entries[i].key, entries[i].value);
                }
            }
        }

        public struct Enumerator : IEnumerator<Entry>
        {
            readonly Map<K, V> map;

            int current;

            internal Enumerator(Map<K, V> map)
            {
                this.map = map;
                current = -1;
            }

            public bool MoveNext()
            {
                return ++current < map.Count;
            }

            public void Reset()
            {
                current = -1;
            }

            public Entry Current => map.entries[current];

            object IEnumerator.Current => map.entries[current];

            public void Dispose()
            {
            }
        }


        /// <summary>
        /// A single entry can hold one ore multiple values, as indicated by size.
        /// </summary>
        public struct Entry : IKeyable<K>
        {
            readonly int code; // lower 31 bits of hash code

            internal readonly K key; // entry key

            internal V value; // entry value

            internal readonly int next; // index of next entry, -1 if last

            internal int tail; // the index of group tail, when this is the head entry

            internal Entry(int code, int next, K key, V value)
            {
                this.code = code;
                this.next = next;
                this.key = key;
                this.value = value;
                tail = -1;
            }

            internal bool Match(int code, K key)
            {
                return this.code == code && this.key.Equals(key);
            }

            public override string ToString()
            {
                return key.ToString();
            }

            public K Key => key;

            public V Value => value;

            public bool IsHead => tail > -1;
        }
    }
}