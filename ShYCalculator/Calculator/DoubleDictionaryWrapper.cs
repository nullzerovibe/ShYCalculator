// -----------------------------------------------------------------------------
// <summary>
//     Performance optimization wrapper to reduce allocations during dictionary lookups.
// </summary>
// -----------------------------------------------------------------------------
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using ShYCalculator.Classes;

namespace ShYCalculator.Calculator;

internal readonly struct DoubleDictionaryWrapper(IEnumerable<KeyValuePair<string, double>>? source) : IDictionary<string, Value> {
    private readonly IEnumerable<KeyValuePair<string, double>> _source = source ?? Enumerable.Empty<KeyValuePair<string, double>>();

    // Read-only implementation
    public Value this[string key] {
        get {
            if (TryGetValue(key, out var value)) return value;
            throw new KeyNotFoundException($"Key '{key}' not found.");
        }
        set => throw new NotSupportedException();
    }

    public ICollection<string> Keys => _source.Select(x => x.Key).ToList();
    public ICollection<Value> Values => _source.Select(x => new Value(DataType.Number, nValue: x.Value)).ToList();
    public int Count => _source.Count();
    public bool IsReadOnly => true;

    [return: NotNullIfNotNull(nameof(key))]
    public bool ContainsKey(string key) {
        // Optimisation for Dictionary or ReadOnlyDictionary
        if (_source is IDictionary<string, double> dict) return dict.ContainsKey(key);
        if (_source is IReadOnlyDictionary<string, double> roDict) return roDict.ContainsKey(key);
        return _source.Any(x => x.Key == key);
    }

    public bool TryGetValue(string key, out Value value) {
        double dVal;
        bool found = false;

        if (_source is IDictionary<string, double> dict) found = dict.TryGetValue(key, out dVal);
        else if (_source is IReadOnlyDictionary<string, double> roDict) found = roDict.TryGetValue(key, out dVal);
        else {
            dVal = 0;
            foreach (var kvp in _source) {
                if (kvp.Key == key) {
                    dVal = kvp.Value;
                    found = true;
                    break;
                }
            }
        }

        if (found) {
            value = new Value(DataType.Number, nValue: dVal);
            return true;
        }

        value = default;
        return false;
    }

    // Not supported / Not needed for read-only wrapper
    public void Add(string key, Value value) => throw new NotSupportedException();
    public bool Remove(string key) => throw new NotSupportedException();
    public void Add(KeyValuePair<string, Value> item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(KeyValuePair<string, Value> item) => throw new NotSupportedException();
    public void CopyTo(KeyValuePair<string, Value>[] array, int arrayIndex) => throw new NotSupportedException();
    public bool Remove(KeyValuePair<string, Value> item) => throw new NotSupportedException();

    public IEnumerator<KeyValuePair<string, Value>> GetEnumerator() {
        foreach (var kvp in _source) {
            yield return new KeyValuePair<string, Value>(kvp.Key, new Value(DataType.Number, nValue: kvp.Value));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
