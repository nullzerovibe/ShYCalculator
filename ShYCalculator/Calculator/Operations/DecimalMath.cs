// -----------------------------------------------------------------------------
// <summary>
//     Exact decimal arithmetic helpers for NumericMode.Decimal.
// </summary>
// -----------------------------------------------------------------------------
using ShY.Classes;

namespace ShY.Calculator.Operations;

internal static class DecimalMath {
    internal static bool TryGetDecimal(in Value value, out decimal result) {
        if (value.Mvalue is { } exact) {
            result = exact;
            return true;
        }
        result = 0;
        return false;
    }

    // WARN: decimal operators throw OverflowException (and DivideByZeroException for 0 ^ -n); callers map both to errors.
    internal static decimal Pow(decimal x, decimal exponent) {
        if (exponent == decimal.Truncate(exponent) && exponent >= int.MinValue && exponent <= int.MaxValue) {
            var n = (long)exponent;
            var negative = n < 0;
            if (negative) n = -n;

            decimal result = 1;
            var factor = x;
            while (n > 0) {
                if ((n & 1) == 1) result *= factor;
                n >>= 1;
                if (n > 0) factor *= factor;
            }
            return negative ? 1 / result : result;
        }

        // WHY: fractional exponents have no exact decimal result; compute in double and convert back.
        if (!Value.TryToDecimal(Math.Pow((double)x, (double)exponent), out var approximated)) {
            throw new OverflowException($"{x} ^ {exponent} is outside the decimal range.");
        }
        return approximated;
    }

    internal static bool TryGetInt64(decimal value, out long result) {
        if (value == decimal.Truncate(value) && value >= long.MinValue && value <= long.MaxValue) {
            result = (long)value;
            return true;
        }
        result = 0;
        return false;
    }
}
