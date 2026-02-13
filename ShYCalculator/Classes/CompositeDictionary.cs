// -----------------------------------------------------------------------------
// <summary>
//     A lightweight wrapper to combine two dictionaries: Primary (Context) and Fallback (Environment).
// </summary>
// -----------------------------------------------------------------------------
using System.Collections;
using ShYCalculator.Classes;

namespace ShYCalculator.Classes;

/// <summary>
/// A lightweight wrapper to combine two dictionaries: Primary (Context) and Fallback (Environment).
/// </summary>
internal readonly struct CompositeDictionary(IDictionary<string, Value> primary, IDictionary<string, Value> fallback) : IDictionary<string, Value> {
    private readonly IDictionary<string, Value> _primary = primary;
    private readonly IDictionary<string, Value> _fallback = fallback;

    public Value this[string key] {
        get {
            if (_primary.TryGetValue(key, out var val)) return val;
            return _fallback[key];
        }
        set => throw new NotSupportedException("CompositeDictionary is read-only");
    }

    public ICollection<string> Keys => _primary.Keys.Concat(_fallback.Keys).Distinct().ToList();
    public ICollection<Value> Values {
        get {
            var keys = Keys;
            var list = new List<Value>(keys.Count);
            var p = _primary;
            var f = _fallback;
            foreach (var key in keys) {
                if (p.TryGetValue(key, out var val)) {
                    list.Add(val);
                }
                else {
                    list.Add(f[key]);
                }
            }
            return list;
        }
    }
    public int Count => Keys.Count;
    public bool IsReadOnly => true;

    public void Add(string key, Value value) => throw new NotSupportedException("CompositeDictionary is read-only");
    public void Add(KeyValuePair<string, Value> item) => throw new NotSupportedException("CompositeDictionary is read-only");
    public void Clear() => throw new NotSupportedException("CompositeDictionary is read-only");
    public bool Contains(KeyValuePair<string, Value> item) => _primary.Contains(item) || _fallback.Contains(item); // Approximation
    public bool ContainsKey(string key) => _primary.ContainsKey(key) || _fallback.ContainsKey(key);
    public void CopyTo(KeyValuePair<string, Value>[] array, int arrayIndex) => throw new NotSupportedException("CompositeDictionary is read-only");
    public bool Remove(string key) => throw new NotSupportedException("CompositeDictionary is read-only");
    public bool Remove(KeyValuePair<string, Value> item) => throw new NotSupportedException("CompositeDictionary is read-only");

    public bool TryGetValue(string key, out Value value) {
        if (_primary.TryGetValue(key, out value)) return true;
        return _fallback.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, Value>> GetEnumerator() {
        foreach (var kvp in _primary) yield return kvp;
        foreach (var kvp in _fallback) {
            if (!_primary.ContainsKey(kvp.Key)) yield return kvp;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
