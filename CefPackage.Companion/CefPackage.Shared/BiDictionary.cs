using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefPackage.Shared
{
    /// <summary>Represents a bidirectional collection of keys and values.</summary>
    /// <typeparam name="TFirst">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TSecond">The type of the values in the dictionary</typeparam>
    [System.Runtime.InteropServices.ComVisible(false)]
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    //[System.Diagnostics.DebuggerTypeProxy(typeof(System.Collections.Generic.Mscorlib_DictionaryDebugView<,>))]
    //[System.Reflection.DefaultMember("Item")]
    public class BiDictionary<TFirst, TSecond> : Dictionary<TFirst, TSecond>
    {
        IDictionary<TSecond, TFirst> _ValueKey = new Dictionary<TSecond, TFirst>();
        /// <summary> PropertyAccessor for Iterator over KeyValue-Relation </summary>
        public IDictionary<TFirst, TSecond> KeyValue => this;
        /// <summary> PropertyAccessor for Iterator over ValueKey-Relation </summary>
        public IDictionary<TSecond, TFirst> ValueKey => _ValueKey;

        #region Implemented members

        /// <Summary>Gets or sets the value associated with the specified key.</Summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <Returns>The value associated with the specified key. If the specified key is not found,
        ///      a get operation throws a <see cref="KeyNotFoundException"/>, and
        ///      a set operation creates a new element with the specified key.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">
        /// The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
        /// <exception cref="T:System.ArgumentException"> An element with the same key already
        /// exists in the <see cref="ValueKey"/> <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</exception>
        public new TSecond this[TFirst key]
        {
            get { return base[key]; }
            set { _ValueKey.Remove(base[key]); base[key] = value; _ValueKey.Add(value, key); }
        }

        /// <Summary>Gets or sets the key associated with the specified value.</Summary>
        /// <param name="val">The value of the key to get or set.</param>
        /// <Returns>The key associated with the specified value. If the specified value is not found,
        ///      a get operation throws a <see cref="KeyNotFoundException"/>, and
        ///      a set operation creates a new element with the specified value.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="val"/> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">
        /// The property is retrieved and <paramref name="val"/> does not exist in the collection.</exception>
        /// <exception cref="T:System.ArgumentException"> An element with the same value already
        /// exists in the <see cref="KeyValue"/> <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</exception>
        public TFirst this[TSecond val]
        {
            get { return _ValueKey[val]; }
            set { base.Remove(_ValueKey[val]); _ValueKey[val] = value; base.Add(value, val); }
        }

        /// <Summary>Adds the specified key and value to the dictionary.</Summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key or value already exists in the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</exception>
        public new void Add(TFirst key, TSecond value)
        {
            base.Add(key, value);
            _ValueKey.Add(value, key);
        }

        /// <Summary>Removes all keys and values from the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</Summary>
        public new void Clear() { base.Clear(); _ValueKey.Clear(); }

        /// <Summary>Determines whether the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/> contains the specified
        ///      KeyValuePair.</Summary>
        /// <param name="item">The KeyValuePair to locate in the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</param>
        /// <Returns>true if the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/> contains an element with
        ///      the specified key which links to the specified value; otherwise, false.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="item"/> is null.</exception>
        public bool Contains(KeyValuePair<TFirst, TSecond> item) => base.ContainsKey(item.Key) & _ValueKey.ContainsKey(item.Value);

        /// <Summary>Removes the specified KeyValuePair from the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</Summary>
        /// <param name="item">The KeyValuePair to remove.</param>
        /// <Returns>true if the KeyValuePair is successfully found and removed; otherwise, false. This
        ///      method returns false if <paramref name="item"/> is not found in the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="item"/> is null.</exception>
        public bool Remove(KeyValuePair<TFirst, TSecond> item) => base.Remove(item.Key) & _ValueKey.Remove(item.Value);

        /// <Summary>Removes the value with the specified key from the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</Summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <Returns>true if the element is successfully found and removed; otherwise, false. This
        ///      method returns false if <paramref name="key"/> is not found in the <see cref="Dictionary&lt;TFirst,TSecond&gt;"/>.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public new bool Remove(TFirst key) => _ValueKey.Remove(base[key]) & base.Remove(key);

        /// <Summary>Gets the key associated with the specified value.</Summary>
        /// <param name="value">The value of the key to get.</param>
        /// <param name="key">When this method returns, contains the key associated with the specified value,
        ///      if the value is found; otherwise, the default value for the type of the key parameter.
        ///      This parameter is passed uninitialized.</param>
        /// <Returns>true if <see cref="ValueKey"/> contains an element with the specified value; 
        ///      otherwise, false.</Returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="value"/> is null.</exception>
        public bool TryGetValue(TSecond value, out TFirst key) => _ValueKey.TryGetValue(value, out key);
        #endregion
    }
}