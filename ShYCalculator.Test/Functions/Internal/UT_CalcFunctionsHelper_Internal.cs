using ShYCalculator.Functions;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Classes;
using ShYCalculator.Functions.Text;
using ShYCalculator.Functions.Dates;


namespace ShYCalculator.Test.Functions.Internal;

[TestClass]
public class UT_CalcFunctionsHelper_Internal {
    [TestMethod]
    public void Test_ReadConfig_Valid() {
        // Numeric functions should exist
        // Assuming CalcScientificFunctions is available or using reflection if needed, 
        // but here we use types available in the project.
        var functions = CalcFunctionsHelper.ReadFunctionsConfiguration("CalcScientificFunctions", typeof(CalcScientificFunctions));
        Assert.IsNotNull(functions);
        Assert.IsNotEmpty(functions);
    }

    [TestMethod]
    public void Test_ReadConfig_AllExtensions_Coverage() {
        // Test UT_CalcFunctionsHelperom main assembly
        var functions = CalcFunctionsHelper.ReadFunctionsConfiguration("CalcScientificFunctions", typeof(CalcScientificFunctions));
        Assert.IsNotNull(functions);
        Assert.IsNotEmpty(functions);
    }

    [TestMethod]
    public void Test_CheckConfig_ValidAllTypes() {
        // Test all valid types: number, array, string, date, boolean, any
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "number" },
                    new() { Type = "string" },
                    new() { Type = "date" },
                    new() { Type = "boolean" },
                    new() { Type = "any" }
                ]
            }
        };
        // Should not throw
        CalcFunctionsHelper.CheckFunctionsConfiguration(functions);
    }

    [TestMethod]
    public void Test_CheckConfig_ValidOptionalArguments() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "number", Optional = false },
                    new() { Type = "string", Optional = true },
                    new() { Type = "date", Optional = true }
                ]
            }
        };
        // Should not throw
        CalcFunctionsHelper.CheckFunctionsConfiguration(functions);
    }

    [TestMethod]
    public void Test_CheckConfig_ValidArrayArgument() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() {
                        Type = "array",
                        Min = 0,
                        Max = 10,
                        Arguments = [
                            new() { Type = "number" }
                        ]
                    }
                ]
            }
        };
        // Should not throw
        CalcFunctionsHelper.CheckFunctionsConfiguration(functions);
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ValidAnyType() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "any" }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 42 });

        // Should not throw
        CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test");
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_OptionalArgument_NotProvided() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number", Optional = false },
                new() { Type = "number", Optional = true }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 1 });

        // Should not throw - optional argument is not provided
        CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test");
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_ValidCount() {
        var function = new CalcFunction {
            Name = "calculateSingleThing",
            Arguments = [
                new() { Type = "string" },
                new() { Type = "number" },
                new() {
                    Type = "array",
                    Min = 0,
                    Max = 10,
                    Optional = true,
                    Arguments = [
                        new() { Type = "date" },
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new Value[] {
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 1 }
        };

        // Should not throw - args match required Types (String, Number) and Array is optional
        CalcFunctionsHelper.CheckFunctionArguments(function, args, "test");
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_MinZero_NoArgs() {
        var function = new CalcFunction {
            Name = "calculateSingleThing",
            Arguments = [
                new() { Type = "string" },
                new() { Type = "number" },
                new() {
                    Type = "array",
                    Min = 0,
                    Max = 5,
                    Optional = true,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new Value[] {
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 1 }
        };

        // Should not throw
        CalcFunctionsHelper.CheckFunctionArguments(function, args, "test");
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_OptionalNotProvided() {
        var function = new CalcFunction {
            Name = "calculateSingleThing",
            Arguments = [
                new() { Type = "string" },
                new() { Type = "number" },
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 5,
                    Optional = true,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new Value[] {
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 1 }
        };

        // Should not throw
        CalcFunctionsHelper.CheckFunctionArguments(function, args, "test");
    }

    [TestMethod]
    public void Test_IsValidType_AllValidTypes() {
        var validTypes = new[] { "number", "array", "string", "date", "boolean", "any" };
        foreach (var type in validTypes) {
            var result = CalcFunctionsHelper.IsValidType(type);
            Assert.IsTrue(result, $"Type '{type}' should be valid");
        }
    }

    [TestMethod]
    public void Test_IsValidType_CaseInsensitive() {
        var result = CalcFunctionsHelper.IsValidType("NUMBER");
        Assert.IsTrue(result);

        result = CalcFunctionsHelper.IsValidType("String");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_IsValidType_Fallback() {
        var result = CalcFunctionsHelper.IsValidType("unknown");
        Assert.IsFalse(result);

        result = CalcFunctionsHelper.IsValidType("");
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Test_IsValidArgType_AllValidTypes() {
        // Test number type
        var result = CalcFunctionsHelper.IsValidArgType("number", new Value { DataType = DataType.Number, Nvalue = 42 });
        Assert.IsTrue(result);

        // Test string type
        result = CalcFunctionsHelper.IsValidArgType("string", new Value { DataType = DataType.String, Svalue = "test" });
        Assert.IsTrue(result);

        // Test date type
        result = CalcFunctionsHelper.IsValidArgType("date", new Value { DataType = DataType.Date, Dvalue = DateTime.Now });
        Assert.IsTrue(result);

        // Test boolean type
        result = CalcFunctionsHelper.IsValidArgType("boolean", new Value { DataType = DataType.Boolean, Bvalue = true });
        Assert.IsTrue(result);

        // Test any type
        result = CalcFunctionsHelper.IsValidArgType("any", new Value());
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_IsValidArgType_CaseInsensitive() {
        var result = CalcFunctionsHelper.IsValidArgType("NUMBER", new Value { DataType = DataType.Number, Nvalue = 42 });
        Assert.IsTrue(result);

        result = CalcFunctionsHelper.IsValidArgType("String", new Value { DataType = DataType.String, Svalue = "test" });
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Test_IsValidArgType_Fallback() {
        var result = CalcFunctionsHelper.IsValidArgType("unknown", new Value());
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Test_IsValidArgType_Date_Invalid() {
        var result = CalcFunctionsHelper.IsValidArgType("date", new Value { DataType = DataType.String, Svalue = "not a date" });
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Test_FunctionInfo_ToString_Coverage() {
        var arg = new CalcFunctionArgument { Type = "number", Optional = true };

        var argString = arg.ToString();
        Assert.AreEqual("Type: number, Optional: True", argString);

        var func = new CalcFunction { Name = "TestFunc" };
        var funcString = func.ToString();
        Assert.AreEqual("Function: TestFunc", funcString);

        var unnamedFunc = new CalcFunction { Name = null };
        var unnamedString = unnamedFunc.ToString();
        Assert.AreEqual("Function: Unnamed", unnamedString);
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_OptionalNotProvided_Coverage() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number" },
                new() { Type = "string", Optional = true }
            ]
        };

        var args = new[] {
            new Value { DataType = DataType.Number, Nvalue = 42 }
        };

        CalcFunctionsHelper.CheckArgTypes(function, args, "test");
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_NullTypeHandling() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = null, Optional = false }
            ]
        };

        var args = new[] {
            new Value { DataType = DataType.String, Svalue = "test" } // Should match default "string" type
        };

        // Should not throw
        CalcFunctionsHelper.CheckArgTypes(function, args, "test");
    }
}
