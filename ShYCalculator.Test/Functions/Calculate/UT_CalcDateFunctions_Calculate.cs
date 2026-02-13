using ShYCalculator.Classes;
using ShYCalculator.Functions.Dates;
using Environment = ShYCalculator.Calculator.Environment;

namespace ShYCalculator.Test.Functions.Calculate;

[TestClass]
public class UT_CalcDateFunctions_Calculate {
    private ShYCalculator m_shyCalculator = null!;
    private Environment m_environment = null!;

    [TestInitialize]
    public void Setup() {
        m_shyCalculator = new ShYCalculator();
        m_environment = (Environment)m_shyCalculator.Environment;

        m_environment.RegisterFunctions(new CalcDateFunctions());
    }

    private void CheckDate(string expression, DateTime expected) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Date, result.DataType, $"Expected Date for '{expression}'");
        var diff = (result.Dvalue!.Value.DateTime - expected).Duration();
        Assert.AreEqual(0.0, diff.TotalSeconds, 1.0, $"Wrong date for '{expression}'. Expected {expected}, got {result.Dvalue!.Value}");
    }

    private void CheckNumber(string expression, double expected, double delta = 1e-9) {
        var result = m_shyCalculator.Calculate(expression);
        Assert.IsTrue(result.Success, $"Calculation failed for '{expression}': {result.Message}");
        Assert.AreEqual(DataType.Number, result.DataType, $"Expected Number for '{expression}'");
        Assert.AreEqual(expected, result.Nvalue!.Value, delta, $"Wrong result for '{expression}'");
    }

    [TestMethod]
    public void Date_Now_Extensive() {
        var before = DateTime.Now;
        var result = m_shyCalculator.Calculate("dt_now()");
        var after = DateTime.Now;

        Assert.IsTrue(result.Success);
        Assert.AreEqual(DataType.Date, result.DataType);
        // Allow small buffer
        Assert.IsTrue(result.Dvalue!.Value >= before.AddSeconds(-1));
        Assert.IsTrue(result.Dvalue!.Value <= after.AddSeconds(1));
    }

    [TestMethod]
    public void Date_Today_Extensive() {
        var today = DateTime.Today;
        CheckDate("dt_today()", today);
    }

    [TestMethod]
    public void Date_Create_Extensive() {
        CheckDate("dt_create(2023, 1, 1)", new DateTime(2023, 1, 1));
        CheckDate("dt_create(2024, 2, 29)", new DateTime(2024, 2, 29)); // Leap year
        CheckDate("dt_create(1999, 12, 31)", new DateTime(1999, 12, 31));
        CheckDate("dt_create(2000, 1, 1, 12, 30, 0)", new DateTime(2000, 1, 1, 12, 30, 0));
        CheckDate("dt_create(2000, 1, 1, 0, 0, 0)", new DateTime(2000, 1, 1, 0, 0, 0));
    }

    [TestMethod]
    public void Date_AddDays_Extensive() {
        CheckDate("dt_adddays(dt_create(2023, 1, 1), 1)", new DateTime(2023, 1, 2));
        CheckDate("dt_adddays(dt_create(2023, 1, 1), -1)", new DateTime(2022, 12, 31));
        CheckDate("dt_adddays(dt_create(2023, 1, 1), 365)", new DateTime(2024, 1, 1));
        CheckDate("dt_adddays(dt_create(2023, 1, 1), 0)", new DateTime(2023, 1, 1));
    }

    [TestMethod]
    public void Date_AddHours_Extensive() {
        var baseDate = new DateTime(2023, 1, 1, 12, 0, 0);
        CheckDate("dt_addhours(dt_create(2023, 1, 1, 12, 0, 0), 1)", baseDate.AddHours(1));
        CheckDate("dt_addhours(dt_create(2023, 1, 1, 12, 0, 0), -12)", baseDate.AddHours(-12));
    }

    [TestMethod]
    public void Date_AddMonths_Extensive() {
        CheckDate("dt_addmonths(dt_create(2023, 1, 1), 1)", new DateTime(2023, 2, 1));
        CheckDate("dt_addmonths(dt_create(2023, 1, 31), 1)", new DateTime(2023, 2, 28));
        CheckDate("dt_addmonths(dt_create(2023, 1, 1), 12)", new DateTime(2024, 1, 1));
    }

    [TestMethod]
    public void Date_AddYears_Extensive() {
        CheckDate("dt_addyears(dt_create(2023, 1, 1), 1)", new DateTime(2024, 1, 1));
        CheckDate("dt_addyears(dt_create(2023, 1, 1), -1)", new DateTime(2022, 1, 1));
        CheckDate("dt_addyears(dt_create(2024, 2, 29), 1)", new DateTime(2025, 2, 28));
    }

    // diff_days removed as not in JSON

    [TestMethod]
    public void Date_Components_Extensive() {
        var dt = "dt_create(2023, 12, 25, 15, 30, 45)";
        CheckNumber($"dt_year({dt})", 2023);
        CheckNumber($"dt_month({dt})", 12);
        CheckNumber($"dt_day({dt})", 25);
        CheckNumber($"dt_hour({dt})", 15);
        CheckNumber($"dt_minute({dt})", 30);
        CheckNumber($"dt_second({dt})", 45);
    }

    [TestMethod]
    public void Date_DayOfWeek_Extensive() {
        CheckNumber("dt_dayofweek(dt_create(2023, 1, 1))", 0);
        CheckNumber("dt_dayofweek(dt_create(2023, 1, 2))", 1);
        CheckNumber("dt_dayofweek(dt_create(2023, 1, 7))", 6);
    }
}
