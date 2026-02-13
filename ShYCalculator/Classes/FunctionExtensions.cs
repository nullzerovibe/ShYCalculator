// -----------------------------------------------------------------------------
// <summary>
//     Defines the FunctionExtensions enum for enabling/disabling function groups.
// </summary>
// -----------------------------------------------------------------------------
using System;

namespace ShYCalculator.Classes;

/// <summary>
/// Flags enum for enabling/disabling function groups.
/// </summary>
[Flags]
public enum FunctionExtensions {
    /// <summary>No functions enabled.</summary>
    None = 0,
    /// <summary>Mathematical functions (sin, cos, abs, etc.).</summary>
    Mathematics = 1 << 0,
    /// <summary>Date functions (now, date, etc.).</summary>
    Date = 1 << 1,
    /// <summary>Text functions (len, mid, etc.).</summary>
    Text = 1 << 2,
    /// <summary>Logical functions (if, switch, etc.).</summary>
    Logical = 1 << 3,

    /// <summary>All standard functions enabled.</summary>
    All = Mathematics | Date | Text | Logical
}
