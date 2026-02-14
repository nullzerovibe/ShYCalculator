using ShYCalculator.Classes;
using ShYCalculator.Functions.Dates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcDateFunctions_Internal {
    // Helper to create Value span
    private static ReadOnlySpan<Value> Vars(params object[] items) {
        var list = new Value[items.Length];
        for (int i = 0; i < items.Length; i++) {
            if (items[i] is double d) list[i] = new Value(DataType.Number, nValue: d);
            else if (items[i] is int iVal) list[i] = new Value(DataType.Number, nValue: (double)iVal);
            else if (items[i] is string s) list[i] = new Value(DataType.String, sValue: s);
            else if (items[i] is DateTimeOffset dto) list[i] = new Value(DataType.Date, dValue: dto);
            else if (items[i] is DateTime dt) list[i] = new Value(DataType.Date, dValue: dt);
            else if (items[i] is Value v) list[i] = v;
            else list[i] = new Value(DataType.Number, nValue: null);
        }
        return list.AsSpan();
    }

    private static ReadOnlySpan<Value> Var(object item) => Vars(item);

    [TestMethod]
    public void Internal_GetDate_ArgVariations() {
        // 1 arg (Year) -> 1st Jan of Year
        var dt = CalcDateFunctions.GetDate(Vars(2050.0)).Dvalue!.Value;
        Assert.AreEqual(new DateTime(2050, 1, 1), dt.DateTime);

        // 2 args (Year, Month)
        dt = CalcDateFunctions.GetDate(Vars(1999.0, 12.0)).Dvalue!.Value;
        Assert.AreEqual(new DateTime(1999, 12, 1), dt.DateTime);

        // 3 args (Y, M, D)
        dt = CalcDateFunctions.GetDate(Vars(2000.0, 2.0, 29.0)).Dvalue!.Value;
        Assert.AreEqual(new DateTime(2000, 2, 29), dt.DateTime);

        // 6 args (Full)
        dt = CalcDateFunctions.GetDate(Vars(2100.0, 1.0, 1.0, 23.0, 59.0, 59.0)).Dvalue!.Value;
        Assert.AreEqual(new DateTime(2100, 1, 1, 23, 59, 59), dt.DateTime);

        // Empty -> Returns 1/1/0001 or similar default?
        // Code: arguments.Length >= 1 ? arguments[0] : 1. So 1, 1, 1 -> 0001-01-01
        dt = CalcDateFunctions.GetDate(Vars()).Dvalue!.Value;
        Assert.AreEqual(1, dt.Year);
        Assert.AreEqual(1, dt.Month);
        Assert.AreEqual(1, dt.Day);
    }

    [TestMethod]
    public void Internal_AddFunctions_Coverage() {
        var baseDate = new DateTimeOffset(2023, 6, 15, 12, 0, 0, TimeSpan.Zero);

        // Add Years
        Assert.AreEqual(2025, CalcDateFunctions.AddYears(baseDate, 2.0).Dvalue!.Value.Year);
        Assert.AreEqual(2021, CalcDateFunctions.AddYears(baseDate, -2.0).Dvalue!.Value.Year);

        // Add Months (Cross year boundary)
        Assert.AreEqual(2024, CalcDateFunctions.AddMonths(baseDate, 7.0).Dvalue!.Value.Year); // June + 7 = Jan next year

        // Add Days (Leap Year Crossing)
        var leapBase = new DateTimeOffset(2024, 2, 28, 0, 0, 0, TimeSpan.Zero);
        Assert.AreEqual(29, CalcDateFunctions.AddDays(leapBase, 1.0).Dvalue!.Value.Day);

        // Add Seconds/Minutes/Hours
        Assert.AreEqual(30, CalcDateFunctions.AddSeconds(baseDate, 30.0).Dvalue!.Value.Second);
        Assert.AreEqual(15, CalcDateFunctions.AddMinutes(baseDate, 15.0).Dvalue!.Value.Minute); // 0 + 15
        Assert.AreEqual(14, CalcDateFunctions.AddHours(baseDate, 2.0).Dvalue!.Value.Hour);
    }

    [TestMethod]
    public void Internal_Comparisons_Precision() {
        var d1 = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var d2 = d1.AddTicks(1);

        var v1 = new Value(DataType.Date, dValue: d1);
        var v2 = new Value(DataType.Date, dValue: d2);

        // Precede
        Assert.IsTrue(new CalcDateFunctions().PrecedeDate(v1, v2).Bvalue!.Value);
        Assert.IsFalse(new CalcDateFunctions().PrecedeDate(v2, v1).Bvalue!.Value);

        // Succeed
        Assert.IsTrue(new CalcDateFunctions().SucceedDate(v2, v1).Bvalue!.Value);

        // Equal
        Assert.IsFalse(new CalcDateFunctions().MatchDate(v1, v2).Bvalue!.Value);
        Assert.IsTrue(new CalcDateFunctions().MatchDate(v1, v1).Bvalue!.Value);

        // String interop in comparison
        var vs = new Value(DataType.String, sValue: "01/01/2023 12:00:00");

        // Note: Default format in code is dd/MM/yyyy. "01/01/2023 12:00:00" might fail or parse if format allows time.
        var vsStrict = new Value(DataType.String, sValue: "01/01/2023");

        // Target for Noon (matches "01/01/2023 12:00:00")
        var localDtNoon = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var offsetNoon = TimeZoneInfo.Local.GetUtcOffset(localDtNoon);
        var dTargetNoon = new DateTimeOffset(localDtNoon, offsetNoon);
        var vTargetNoon = new Value(DataType.Date, dValue: dTargetNoon);

        // Target for Midnight (matches "01/01/2023")
        var localDtMidnight = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var offsetMidnight = TimeZoneInfo.Local.GetUtcOffset(localDtMidnight);
        var dTargetMidnight = new DateTimeOffset(localDtMidnight, offsetMidnight);
        var vTargetMidnight = new Value(DataType.Date, dValue: dTargetMidnight);

        // Use custom format for time component test
        Assert.IsTrue(new CalcDateFunctions("dd/MM/yyyy HH:mm:ss").MatchDate(vs, vTargetNoon).Bvalue!.Value);
        Assert.IsTrue(new CalcDateFunctions().MatchDate(vsStrict, vTargetMidnight).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_AllDateValues_Coverage() {
        // Valid Mixed (Date + String)
        Assert.IsTrue(new CalcDateFunctions().AllDateValues(Vars(
            DateTimeOffset.Now,
            "31/12/2022"
        )).Bvalue!.Value);

        // Invalid String
        Assert.IsFalse(new CalcDateFunctions().AllDateValues(Vars(
            DateTimeOffset.Now,
            "invalid-date"
        )).Bvalue!.Value);

        // Empty
        Assert.IsFalse(new CalcDateFunctions().AllDateValues(Vars()).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_IntervalType_Parsing() {
        // Internal static method test if accessible
        Assert.AreEqual(CalcDateFunctions.IntervalType.Year, CalcDateFunctions.GetIntervalType("2y"));
        Assert.AreEqual(CalcDateFunctions.IntervalType.Month, CalcDateFunctions.GetIntervalType("12m"));
        Assert.AreEqual(CalcDateFunctions.IntervalType.Day, CalcDateFunctions.GetIntervalType("365d"));
        Assert.AreEqual(CalcDateFunctions.IntervalType.FirstOfDecemberCY, CalcDateFunctions.GetIntervalType("1st of December of current year")); // precise

        try { CalcDateFunctions.GetIntervalType("2x"); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_GetIntervalNumericValue_Coverage() {
        Assert.AreEqual(10, CalcDateFunctions.GetIntervalNumericValue("10d"));
        Assert.AreEqual(-5, CalcDateFunctions.GetIntervalNumericValue("-5m"));

        try { CalcDateFunctions.GetIntervalNumericValue("yd"); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_Constructor_Coverage() {
        // Custom date format constructor
        var funcs = new CalcDateFunctions("MM-dd-yyyy");
        Assert.AreEqual("CalcDateFunctions", funcs.Name);

        // Verify format usage (indirectly via AllDates or explicit Parse if exposed, 
        // but internal m_stringDateFormat is private. 
        // We can test via AllDates which uses the format)
        Assert.IsTrue(funcs.AllDateValues(Vars("12-31-2023")).Bvalue!.Value);
        Assert.IsFalse(funcs.AllDateValues(Vars("31/12/2023")).Bvalue!.Value); // Default format should fail
    }

    [TestMethod]
    public void Internal_Dispatch_Coverage() {
        var funcs = new CalcDateFunctions();
        var dt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Explicitly calling ExecuteFunction to hit the lambdas
        Assert.IsNotNull(funcs.ExecuteFunction("dt_create", Vars(2023)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_now", Vars()));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_today", Vars()));
        Assert.AreEqual(1.0, funcs.ExecuteFunction("dt_dayofyear", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(0.0, funcs.ExecuteFunction("dt_dayofweek", Vars(dt)).Nvalue!.Value); // Sunday
        Assert.AreEqual(2023.0, funcs.ExecuteFunction("dt_year", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(1.0, funcs.ExecuteFunction("dt_month", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(1.0, funcs.ExecuteFunction("dt_day", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(0.0, funcs.ExecuteFunction("dt_hour", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(0.0, funcs.ExecuteFunction("dt_minute", Vars(dt)).Nvalue!.Value);
        Assert.AreEqual(0.0, funcs.ExecuteFunction("dt_second", Vars(dt)).Nvalue!.Value);

        Assert.IsNotNull(funcs.ExecuteFunction("dt_add", Vars(dt, "1d")));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_addyears", Vars(dt, 1)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_addmonths", Vars(dt, 1)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_adddays", Vars(dt, 1)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_addhours", Vars(dt, 1)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_addminutes", Vars(dt, 1)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_addseconds", Vars(dt, 1)));

        Assert.IsNotNull(funcs.ExecuteFunction("dt_before", Vars(dt, dt.AddDays(1))));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_after", Vars(dt.AddDays(1), dt)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_equal", Vars(dt, dt)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_before_equal", Vars(dt, dt)));
        Assert.IsNotNull(funcs.ExecuteFunction("dt_after_equal", Vars(dt, dt)));
        Assert.IsTrue(funcs.ExecuteFunction("dt_all", Vars(dt)).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_EdgeCase_Coverage() {
        // AddDateInterval - nulls/invalid
        try { CalcDateFunctions.AddDateInterval(null, "1d"); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddDateInterval(DateTimeOffset.Now, null!); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddDateInterval(DateTimeOffset.Now, ""); Assert.Fail(); } catch (FunctionException) { }

        // Get* functions - nulls
        try { CalcDateFunctions.GetDayOfYear(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetDayOfWeek(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetYear(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetMonth(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetDay(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetHour(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetMinute(null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.GetSecond(null); Assert.Fail(); } catch (FunctionException) { }

        // Add* functions - nulls
        try { CalcDateFunctions.AddYears(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddYears(null, 1); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddMonths(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddMonths(null, 1); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddDays(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddDays(null, 1); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddHours(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddHours(null, 1); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddMinutes(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddMinutes(null, 1); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddSeconds(DateTimeOffset.Now, null); Assert.Fail(); } catch (FunctionException) { }
        try { CalcDateFunctions.AddSeconds(null, 1); Assert.Fail(); } catch (FunctionException) { }
    }

    [TestMethod]
    public void Internal_Date_DataTypeMismatch() {
        var funcs = new CalcDateFunctions();
        // Use DataType.Date but with null Dvalue to pass CheckFunctionArguments but fail TryParseDateValue
        var invalidDateVal = new Value(DataType.Date, dValue: null);
        var validDateVal = new Value(DataType.Date, dValue: DateTimeOffset.Now);

        try { funcs.ExecuteFunction("dt_before", Vars(invalidDateVal, validDateVal)); Assert.Fail("dt_before did not throw"); } catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message, $"Wrong message: {ex.Message}"); }
        try { funcs.ExecuteFunction("dt_after", Vars(validDateVal, invalidDateVal)); Assert.Fail("dt_after did not throw"); } catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message, $"Wrong message: {ex.Message}"); }
        try { funcs.ExecuteFunction("dt_equal", Vars(invalidDateVal, invalidDateVal)); Assert.Fail("dt_equal did not throw"); } catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message, $"Wrong message: {ex.Message}"); }
    }

    [TestMethod]
    public void Internal_Date_IntervalType_EdgeCases() {
        // 2. Coverage for GetIntervalType default case (invalid suffix)
        try { CalcDateFunctions.GetIntervalType("10x"); Assert.Fail("GetIntervalType did not throw"); } catch (FunctionException ex) { Assert.Contains("Invalid interval type", ex.Message, $"Wrong message: {ex.Message}"); }

        // 3. Coverage for GetIntervalNumericValue int.TryParse failure
        try { CalcDateFunctions.GetIntervalNumericValue("9999999999999d"); Assert.Fail("GetIntervalNumericValue did not throw"); } catch (FunctionException ex) { Assert.Contains("Invalid interval numeric value", ex.Message, $"Wrong message: {ex.Message}"); }
    }

    [TestMethod]
    public void Internal_Date_AllDateValues_False() {
        var funcs = new CalcDateFunctions();
        // 4. Coverage for AllDateValues returning false
        Assert.IsFalse(funcs.ExecuteFunction("dt_all", Vars(123)).Bvalue!.Value, "dt_all(123) should be false");
        Assert.IsFalse(funcs.ExecuteFunction("dt_all", Vars("not-a-date")).Bvalue!.Value, "dt_all(not-a-date) should be false");
        Assert.IsFalse(funcs.ExecuteFunction("dt_all", Vars(DateTimeOffset.Now, "bad")).Bvalue!.Value, "dt_all(mixed) should be false");
    }

    [TestMethod]
    public void Internal_Date_SpecialInterval() {
        var funcs = new CalcDateFunctions();
        // 5. Explicitly hit the "1st of December" special interval type if missed
        var dec1 = funcs.ExecuteFunction("dt_add", Vars(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "1st of December of current year"));
        Assert.AreEqual(12, dec1.Dvalue!.Value.Month);
        Assert.AreEqual(1, dec1.Dvalue!.Value.Day);
        Assert.AreEqual(2023, dec1.Dvalue!.Value.Year);
    }

    [TestMethod]
    public void Internal_DeepCoverage_Phase2() {
        var funcs = new CalcDateFunctions();

        // 1. AddDateInterval Switch Coverage
        Assert.AreEqual(2024, funcs.ExecuteFunction("dt_add", Vars(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "1y")).Dvalue!.Value.Year);
        Assert.AreEqual(2, funcs.ExecuteFunction("dt_add", Vars(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "1m")).Dvalue!.Value.Month);
        Assert.AreEqual(2, funcs.ExecuteFunction("dt_add", Vars(new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero), "1d")).Dvalue!.Value.Day);

        // 2. TryParseDateValue false path with non-null SValue
        // Masquerade as Date to pass CheckFunctionArguments, but provide invalid string to fail TryParse
        var badStringVal = new Value(DataType.Date, sValue: "not-a-date");
        var validDate = new Value(DataType.Date, dValue: DateTimeOffset.Now);

        try { funcs.ExecuteFunction("dt_before", Vars(badStringVal, validDate)); Assert.Fail("Should throw"); }
        catch (FunctionException ex) { Assert.IsTrue(ex.Message.Contains("Invalid arguments") || ex.Message.Contains("Invalid argument type"), $"Wrong message: {ex.Message}"); }

        // 3. SucceedDate/PrecedeDate Right-Operand Null Coverage
        try { funcs.ExecuteFunction("dt_after", Vars(validDate, badStringVal)); Assert.Fail("Should throw"); }
        catch (FunctionException ex) { Assert.Contains("Right date is null", ex.Message); }

        try { funcs.ExecuteFunction("dt_before", Vars(validDate, badStringVal)); Assert.Fail("Should throw"); }
        catch (FunctionException ex) { Assert.Contains("Right date is null", ex.Message); }

        try { funcs.ExecuteFunction("dt_equal", Vars(validDate, badStringVal)); Assert.Fail("Should throw"); }
        catch (FunctionException ex) { Assert.Contains("Right date is null", ex.Message); }

        // 4. SucceedOrMatch / PrecedeOrMatch Logical Paths
        var dt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.IsTrue(funcs.ExecuteFunction("dt_before_equal", Vars(dt, dt)).Bvalue!.Value);
        Assert.IsTrue(funcs.ExecuteFunction("dt_after_equal", Vars(dt, dt)).Bvalue!.Value);

        Assert.IsTrue(funcs.ExecuteFunction("dt_before_equal", Vars(dt, dt.AddDays(1))).Bvalue!.Value);
        Assert.IsTrue(funcs.ExecuteFunction("dt_after_equal", Vars(dt.AddDays(1), dt)).Bvalue!.Value);

        Assert.IsFalse(funcs.ExecuteFunction("dt_before_equal", Vars(dt.AddDays(1), dt)).Bvalue!.Value);
        Assert.IsFalse(funcs.ExecuteFunction("dt_after_equal", Vars(dt, dt.AddDays(1))).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_DeepCoverage_Phase3() {
        var funcs = new CalcDateFunctions();

        // 1. SucceedDate (dt_after) Left-Operand Null Coverage
        // Previously we only tested Left-Valid, Right-Invalid.
        // We need Left-Invalid to hit the first part of the || condition.
        var invalidDate = new Value(DataType.Date, dValue: null);
        var validDate = new Value(DataType.Date, dValue: DateTimeOffset.Now);

        try { funcs.ExecuteFunction("dt_after", Vars(invalidDate, validDate)); Assert.Fail("Should throw"); }
        catch (FunctionException ex) { Assert.Contains("Left date is null", ex.Message); }

        // 2. TryParseDateValue with Valid String Date
        // We need to bypass CheckFunctionArguments (which rejects String for dt_before)
        // by calling the internal method PrecedeDate directly.

        var stringDate = new Value(DataType.String, sValue: "01/01/2023");
        var compareDate = new Value(DataType.Date, dValue: new DateTimeOffset(2023, 1, 2, 0, 0, 0, TimeSpan.Zero));

        // "01/01/2023" is before "02/01/2023"
        // PrecedeDate("01/01/2023", 2023-01-02) -> True
        Assert.IsTrue(funcs.PrecedeDate(stringDate, compareDate).Bvalue!.Value);

        // Also verify the other way around to ensure both args can be strings logic works
        var stringDate2 = new Value(DataType.String, sValue: "02/01/2023");
        Assert.IsTrue(funcs.PrecedeDate(stringDate, stringDate2).Bvalue!.Value);
    }

    [TestMethod]
    public void Internal_DeepCoverage_Phase4() {
        var funcs = new CalcDateFunctions();
        var validDate = new Value(DataType.Date, dValue: DateTimeOffset.Now);

        // 1. Pass Number to TryParseDateValue (via PrecedeDate)
        // Number has Dvalue=null, Svalue=null. Should skip both checks and return false.
        var numVal = new Value(DataType.Number, nValue: 123);
        try { funcs.PrecedeDate(numVal, validDate); Assert.Fail("Should throw for Number"); }
        catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message); }

        // 2. Pass Boolean to TryParseDateValue
        // Boolean has Dvalue=null, Svalue=null. Should skip both checks and return false.
        var boolVal = new Value(DataType.Boolean, bValue: true);
        try { funcs.PrecedeDate(boolVal, validDate); Assert.Fail("Should throw for Boolean"); }
        catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message); }
    }

    [TestMethod]
    public void Internal_DeepCoverage_Phase5() {
        var funcs = new CalcDateFunctions();
        var validDate = new Value(DataType.Date, dValue: DateTimeOffset.Now);

        // Explicitly Cover: Svalue != null BUT TryParseExact fails
        // Requires DataType.String so Value.Svalue returns the string.
        // Requires Direct Call to bypass CheckFunctionArguments.

        var badStringVal = new Value(DataType.String, sValue: "invalid-date-string");

        // PrecedeDate(badString, validDate)
        // -> TryParseDateValue(badString)
        //    -> Dvalue is null
        //    -> Svalue is "invalid-date-string" (Entered if block)
        //    -> TryParseExact("invalid-date-string") -> False
        //    -> Exit if block
        // -> return false
        // -> PrecedeDate sees success1=false -> Throws "Invalid arguments"

        try { funcs.PrecedeDate(badStringVal, validDate); Assert.Fail("Should throw for invalid string"); }
        catch (FunctionException ex) { Assert.Contains("Invalid arguments", ex.Message); }
    }
}
