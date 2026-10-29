// -----------------------------------------------------------------------------
// <summary>
//     Allocation-free key lookups by ReadOnlySpan<char> over caller-supplied dictionaries.
// </summary>
// -----------------------------------------------------------------------------
using System.Collections.Frozen;

namespace ShY.Classes;

/// <summary>Implemented by internal dictionary adapters that can resolve keys without materializing a string.</summary>
internal interface ISpanLookup<TValue> {
    bool TryGetValue(ReadOnlySpan<char> key, out TValue value);
}

internal static class SpanLookup {
    // WHY: span lookups are .NET 9+; on net8.0 every path falls back to a string lookup.
    // WARN: GetAlternateLookup throws for comparers without span support (any custom IEqualityComparer); always use the Try* form.
    internal static bool TryGetValue<TValue>(IDictionary<string, TValue> source, ReadOnlySpan<char> key, out TValue value) {
        switch (source) {
#if NET9_0_OR_GREATER
            case Dictionary<string, TValue> dictionary when dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup):
                return lookup.TryGetValue(key, out value!);
            case FrozenDictionary<string, TValue> frozen when frozen.TryGetAlternateLookup<ReadOnlySpan<char>>(out var frozenLookup):
                return frozenLookup.TryGetValue(key, out value!);
#endif
            case ISpanLookup<TValue> spanLookup:
                return spanLookup.TryGetValue(key, out value);
            default:
                return source.TryGetValue(key.ToString(), out value!);
        }
    }

    internal static bool TryGetKey<TValue>(IDictionary<string, TValue> source, ReadOnlySpan<char> key, out string actualKey) {
#if NET9_0_OR_GREATER
        if (source is Dictionary<string, TValue> dictionary && dictionary.TryGetAlternateLookup<ReadOnlySpan<char>>(out var lookup)) {
            return lookup.TryGetValue(key, out actualKey!, out _);
        }
#endif

        var candidate = key.ToString();
        if (source.ContainsKey(candidate)) {
            actualKey = candidate;
            return true;
        }

        actualKey = "";
        return false;
    }
}
