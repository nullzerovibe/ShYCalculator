// -----------------------------------------------------------------------------
// <summary>
//     Implements date and time functions (now, today, date, year, month, day, add_days, etc.).
// </summary>
// -----------------------------------------------------------------------------
using ShYCalculator.Classes;
using System.Reflection;
using System.Collections.Frozen;

namespace ShYCalculator.Functions.Dates;

/// <summary>
/// Implements date and time functions (now, today, date, year, month, day, add_days, etc.).
/// </summary>
public class CalcDateFunctions : ICalcFunctionsExtension {
    #region Members
    private readonly FrozenDictionary<string, CalcFunction> m_funcDef;
    private readonly FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> m_dispatch;
    private readonly string m_stringDateFormat;
    #endregion Members

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcDateFunctions"/> class.
    /// </summary>
    public CalcDateFunctions() {
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(CalcDateFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);
        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_stringDateFormat = "dd/MM/yyyy";
        m_dispatch = CreateDispatch();
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="CalcDateFunctions"/> class with a specific date format.
    /// </summary>
    /// <param name="stringDateFormat">Date format string for parsing.</param>
    public CalcDateFunctions(string stringDateFormat) {
        var funcList = CalcFunctionsHelper.ReadFunctionsConfiguration(Name, typeof(CalcDateFunctions));
        CalcFunctionsHelper.CheckFunctionsConfiguration(funcList);
        m_funcDef = funcList.ToDictionary(x => x.Name!, StringComparer.OrdinalIgnoreCase).ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        m_stringDateFormat = stringDateFormat;
        m_dispatch = CreateDispatch();
    }

    private FrozenDictionary<string, Func<ReadOnlySpan<Value>, Value>> CreateDispatch() {
        return new Dictionary<string, Func<ReadOnlySpan<Value>, Value>>(StringComparer.OrdinalIgnoreCase) {
            [FunctionNames.Date] = p => GetDate(p, FunctionNames.Date),
            [FunctionNames.Now] = _ => GetNow(FunctionNames.Now),
            [FunctionNames.Today] = _ => GetToday(FunctionNames.Today),
            [FunctionNames.DayOfYear] = p => GetDayOfYear(p[0].Dvalue, FunctionNames.DayOfYear),
            [FunctionNames.DayOfWeek] = p => GetDayOfWeek(p[0].Dvalue, FunctionNames.DayOfWeek),
            [FunctionNames.Year] = p => GetYear(p[0].Dvalue, FunctionNames.Year),
            [FunctionNames.Month] = p => GetMonth(p[0].Dvalue, FunctionNames.Month),
            [FunctionNames.Day] = p => GetDay(p[0].Dvalue, FunctionNames.Day),
            [FunctionNames.Hour] = p => GetHour(p[0].Dvalue, FunctionNames.Hour),
            [FunctionNames.Minute] = p => GetMinute(p[0].Dvalue, FunctionNames.Minute),
            [FunctionNames.Second] = p => GetSecond(p[0].Dvalue, FunctionNames.Second),

            [FunctionNames.DateAdd] = p => AddDateInterval(p[0].Dvalue, p[1].Svalue, FunctionNames.DateAdd),
            [FunctionNames.AddYears] = p => AddYears(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddYears),
            [FunctionNames.AddMonths] = p => AddMonths(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddMonths),
            [FunctionNames.AddDays] = p => AddDays(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddDays),
            [FunctionNames.AddHours] = p => AddHours(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddHours),
            [FunctionNames.AddMinutes] = p => AddMinutes(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddMinutes),
            [FunctionNames.AddSeconds] = p => AddSeconds(p[0].Dvalue, p[1].Nvalue, FunctionNames.AddSeconds),

            [FunctionNames.IsBefore] = p => PrecedeDate(p[0], p[1], FunctionNames.IsBefore),
            [FunctionNames.IsAfter] = p => SucceedDate(p[0], p[1], FunctionNames.IsAfter),
            [FunctionNames.IsSameDate] = p => MatchDate(p[0], p[1], FunctionNames.IsSameDate),
            [FunctionNames.IsBeforeOrSame] = p => PrecedeOrMatchDate(p[0], p[1], FunctionNames.IsBeforeOrSame),
            [FunctionNames.IsAfterOrSame] = p => SucceedOrMatchDate(p[0], p[1], FunctionNames.IsAfterOrSame),
            [FunctionNames.AllDates] = p => AllDateValues(p, FunctionNames.AllDates)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
    #endregion Constructor

    #region Properties
    /// <inheritdoc/>
    public string Name => "CalcDateFunctions";
    #endregion Properties

    #region Methods
    /// <inheritdoc/>
    public IEnumerable<CalcFunction> GetFunctions() {
        return m_funcDef.Values;
    }

    /// <inheritdoc/>
    public Value ExecuteFunction(string functionName, ReadOnlySpan<Value> parameters) {
        if (!m_funcDef.TryGetValue(functionName, out var functionDef)) {
            throw new FunctionException($"Invalid Function name in extension {Name} on function {functionName}");
        }

        CalcFunctionsHelper.CheckFunctionArguments(functionDef, parameters, Name);

        if (m_dispatch.TryGetValue(functionName, out var function)) {
            return function(parameters);
        }

        throw new FunctionException($"Invalid Function name in extension {Name} on function {functionName}");
    }
    #endregion Methods

    #region Private Get Functions
    internal static Value GetDate(ReadOnlySpan<Value> arguments, string _ = FunctionNames.Date) {
        var year = (int)(arguments.Length >= 1 ? arguments[0].Nvalue ?? 1 : 1);
        var month = (int)(arguments.Length >= 2 ? arguments[1].Nvalue ?? 1 : 1);
        var day = (int)(arguments.Length >= 3 ? arguments[2].Nvalue ?? 1 : 1);
        var hour = (int)(arguments.Length >= 4 ? arguments[3].Nvalue ?? 0 : 0);
        var minute = (int)(arguments.Length >= 5 ? arguments[4].Nvalue ?? 0 : 0);
        var seconds = (int)(arguments.Length >= 6 ? arguments[5].Nvalue ?? 0 : 0);

        return new Value(DataType.Date, dValue: new DateTimeOffset(year, month, day, hour, minute, seconds, TimeSpan.Zero));
    }
    #endregion Private Get Functions

    #region Rest of date functions updated to Span
    // (Existing methods like GetNow, GetToday etc. don't need span updates if they take no args or single Dvalue?)
    // Actually, I'll just replace the relevant parts of the file.

    private static Value GetNow(string _ = FunctionNames.Now) {
        return new Value(DataType.Date, dValue: DateTimeOffset.Now);
    }

    private static Value GetToday(string _ = FunctionNames.Today) {
        var now = DateTimeOffset.Now;
        return new Value(DataType.Date, dValue: new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset));
    }

    internal static Value GetDayOfYear(DateTimeOffset? date, string functionName = FunctionNames.DayOfYear) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.DayOfYear);
    }

    internal static Value GetDayOfWeek(DateTimeOffset? date, string functionName = FunctionNames.DayOfWeek) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: (int)date.Value.DayOfWeek);
    }

    internal static Value GetYear(DateTimeOffset? date, string functionName = FunctionNames.Year) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Year);
    }

    internal static Value GetMonth(DateTimeOffset? date, string functionName = FunctionNames.Month) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Month);
    }

    internal static Value GetDay(DateTimeOffset? date, string functionName = FunctionNames.Day) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Day);
    }

    internal static Value GetHour(DateTimeOffset? date, string functionName = FunctionNames.Hour) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Hour);
    }

    internal static Value GetMinute(DateTimeOffset? date, string functionName = FunctionNames.Minute) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Minute);
    }

    internal static Value GetSecond(DateTimeOffset? date, string functionName = FunctionNames.Second) {
        if (!date.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Number, nValue: date.Value.Second);
    }
    #endregion Private Get Functions

    #region Private Add Functions
    internal static Value AddDateInterval(DateTimeOffset? date, string? interval, string functionName = FunctionNames.DateAdd) {
        if (date == null || interval == null || interval.Length < 2) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        var intervalType = GetIntervalType(interval, functionName);
        if (intervalType == IntervalType.FirstOfDecemberCY) {
            return new Value(DataType.Date, dValue: new DateTimeOffset(date.Value.Year, 12, 1, date.Value.Hour, date.Value.Minute, date.Value.Second, date.Value.Offset));
        }

        var intervalNumber = GetIntervalNumericValue(interval, functionName);

        return intervalType switch {
            IntervalType.Year => AddYears(date, intervalNumber, functionName),
            IntervalType.Month => AddMonths(date, intervalNumber, functionName),
            _ => AddDays(date, intervalNumber, functionName)
        };
    }

    internal static Value AddYears(DateTimeOffset? date, double? years, string functionName = FunctionNames.AddYears) {
        if (!date.HasValue || !years.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddYears((int)years));
    }

    internal static Value AddMonths(DateTimeOffset? date, double? months, string functionName = FunctionNames.AddMonths) {
        if (!date.HasValue || !months.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddMonths((int)months));
    }

    internal static Value AddDays(DateTimeOffset? date, double? days, string functionName = FunctionNames.AddDays) {
        if (!date.HasValue || !days.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddDays(days.Value));
    }

    internal static Value AddHours(DateTimeOffset? date, double? hours, string functionName = FunctionNames.AddHours) {
        if (!date.HasValue || !hours.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddHours(hours.Value));
    }

    internal static Value AddMinutes(DateTimeOffset? date, double? minutes, string functionName = FunctionNames.AddMinutes) {
        if (!date.HasValue || !minutes.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddMinutes(minutes.Value));
    }

    internal static Value AddSeconds(DateTimeOffset? date, double? seconds, string functionName = FunctionNames.AddSeconds) {
        if (!date.HasValue || !seconds.HasValue) {
            throw new FunctionException($"Invalid arguments on function {functionName}.");
        }

        return new Value(DataType.Date, dValue: date.Value.AddSeconds(seconds.Value));
    }
    #endregion Private Add Functions

    #region Private Other Functions
    internal Value PrecedeOrMatchDate(Value date1, Value date2, string functionName = FunctionNames.IsBeforeOrSame) {
        var pd = PrecedeDate(date1, date2, functionName);
        var md = MatchDate(date1, date2, functionName);

        return new Value(DataType.Boolean, bValue: pd.Bvalue == true || md.Bvalue == true);
    }

    internal Value SucceedOrMatchDate(Value date1, Value date2, string functionName = FunctionNames.IsAfterOrSame) {
        var sd = SucceedDate(date1, date2, functionName);
        var md = MatchDate(date1, date2, functionName);

        return new Value(DataType.Boolean, bValue: sd.Bvalue == true || md.Bvalue == true);
    }

    internal Value SucceedDate(Value date1, Value date2, string functionName = FunctionNames.IsAfter) {
        var _1 = TryParseDateValue(date1, out var firstDate);
        var _2 = TryParseDateValue(date2, out var secondDate);

        if (firstDate is not { } d1 || secondDate is not { } d2) {
            throw new FunctionException(GetDataTypeMismatchErrorMessage(firstDate, secondDate, functionName));
        }

        return new Value(DataType.Boolean, bValue: DateTimeOffset.Compare(d1, d2) > 0);
    }

    internal Value PrecedeDate(Value date1, Value date2, string functionName = FunctionNames.IsBefore) {
        var _1 = TryParseDateValue(date1, out var firstDate);
        var _2 = TryParseDateValue(date2, out var secondDate);

        if (firstDate is not { } d1 || secondDate is not { } d2) {
            throw new FunctionException(GetDataTypeMismatchErrorMessage(firstDate, secondDate, functionName));
        }

        return new Value(DataType.Boolean, bValue: DateTimeOffset.Compare(d1, d2) < 0);
    }

    internal Value MatchDate(Value date1, Value date2, string functionName = FunctionNames.IsSameDate) {
        var _1 = TryParseDateValue(date1, out var firstDate);
        var _2 = TryParseDateValue(date2, out var secondDate);

        if (firstDate is not { } d1 || secondDate is not { } d2) {
            throw new FunctionException(GetDataTypeMismatchErrorMessage(firstDate, secondDate, functionName));
        }

        return new Value(DataType.Boolean, bValue: DateTimeOffset.Compare(d1, d2) == 0);
    }

    internal Value AllDateValues(ReadOnlySpan<Value> arguments, string _1 = FunctionNames.AllDates) {
        if (arguments.Length == 0) return new Value(DataType.Boolean, bValue: false);

        for (int i = 0; i < arguments.Length; i++) {
            var argument = arguments[i];
            if (argument.Dvalue != null) {
                continue;
            }

            if (string.IsNullOrEmpty(argument.Svalue)) {
                return new Value(DataType.Boolean, bValue: false);
            }

            if (!DateTimeOffset.TryParseExact(argument.Svalue, m_stringDateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)) {
                return new Value(DataType.Boolean, bValue: false);
            }
        }

        return new Value(DataType.Boolean, bValue: true);
    }
    #endregion Private Other Functions

    #region Private Methods
    internal string GetDataTypeMismatchErrorMessage(DateTimeOffset? firstDate, DateTimeOffset? secondDate, string functionName) {
        var message = $"Invalid arguments, expected dates.";

        if (firstDate == null) {
            message += $" Left date is null.";
        }

        if (secondDate == null) {
            message += $" Right date is null.";
        }

        message += $" In extension {Name} on function {functionName}.";

        return message;
    }

    private bool TryParseDateValue(Value value, out DateTimeOffset? date) {
        date = null;
        if (value.Dvalue != null) {
            date = value.Dvalue;
            return true;
        }

        if (value.Svalue != null) {
            if (DateTimeOffset.TryParseExact(value.Svalue, m_stringDateFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate)) {
                date = parsedDate;
                return true;
            }
        }

        return false;
    }

    internal static IntervalType GetIntervalType(string interval, string functionName = FunctionNames.DateAdd) {
        if (interval.Length < 2) {
            throw new FunctionException($"Invalid interval format on {functionName}");
        }

        if (string.Equals(interval, "1st of December of current year", StringComparison.OrdinalIgnoreCase)) {
            return IntervalType.FirstOfDecemberCY;
        }

        var lastChar = char.ToLowerInvariant(interval[^1]);

        return lastChar switch {
            'y' or 'a' => IntervalType.Year,
            'm' => IntervalType.Month,
            'd' => IntervalType.Day,
            _ => throw new FunctionException($"Invalid interval type on {functionName}")
        };
    }

    internal static int GetIntervalNumericValue(string interval, string functionName = FunctionNames.DateAdd) {
        if (interval.Length < 2) {
            throw new FunctionException($"Invalid interval format on {functionName}");
        }

        var intervalValueString = interval[..^1];

        if (!int.TryParse(intervalValueString, out var intervalValue)) {
            throw new FunctionException($"Invalid interval numeric value on {functionName}");
        }

        return intervalValue;
    }
    #endregion Private Methods

    internal enum IntervalType {
        Year,
        Month,
        Day,
        FirstOfDecemberCY,
    }
}

internal static class FunctionNames {
    public const string Date = "dt_create";
    public const string Now = "dt_now";
    public const string Today = "dt_today";
    public const string DayOfYear = "dt_dayofyear";
    public const string DayOfWeek = "dt_dayofweek";
    public const string Year = "dt_year";
    public const string Month = "dt_month";
    public const string Day = "dt_day";
    public const string Hour = "dt_hour";
    public const string Minute = "dt_minute";
    public const string Second = "dt_second";
    public const string DateAdd = "dt_add";
    public const string AddYears = "dt_addyears";
    public const string AddMonths = "dt_addmonths";
    public const string AddDays = "dt_adddays";
    public const string AddHours = "dt_addhours";
    public const string AddMinutes = "dt_addminutes";
    public const string AddSeconds = "dt_addseconds";
    public const string IsBefore = "dt_before";
    public const string IsAfter = "dt_after";
    public const string IsSameDate = "dt_equal";
    public const string IsBeforeOrSame = "dt_before_equal";
    public const string IsAfterOrSame = "dt_after_equal";
    public const string AllDates = "dt_all";
}
