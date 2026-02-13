using ShYCalculator.Functions;
using ShYCalculator.Functions.Mathematics;
using ShYCalculator.Classes;
using System.Reflection;

namespace ShYCalculator.Test.Functions.Coverage;

[TestClass]
public class UT_CalcFunctionsHelper_Coverage {
    private static void AssertThrows<T>(Action action) where T : Exception {
        try {
            action();
        }
        catch (T) {
            return;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is T) {
            return;
        }
        catch (Exception ex) {
            throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but got {ex.GetType().Name}. Message: {ex.Message}");
        }
        throw new AssertFailedException($"Expected exception of type {typeof(T).Name} but no exception was thrown");
    }

    [TestMethod]
    public void Test_ReadConfig_InvalidResource() {
        // This should fail because ShYCalculator.Functions.Mathematics.Invalid.json doesn't exist
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.ReadFunctionsConfiguration("Invalid", typeof(CalcScientificFunctions)));
    }

    [TestMethod]
    public void Test_ReadConfig_WrongType_Throws() {
        // Passing a type from an assembly/namespace that doesn't contain the resource
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.ReadFunctionsConfiguration("CalcArithmeticalFunctions", typeof(UT_CalcFunctionsHelper_Coverage)));
    }

    [TestMethod]
    public void Test_CheckConfig_EmptyList() {
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration([]));
    }

    [TestMethod]
    public void Test_CheckConfig_MissingName() {
        var functions = new List<CalcFunction> {
            new() { Name = null, Description = "Test" }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_EmptyName() {
        var functions = new List<CalcFunction> {
            new() { Name = "", Description = "Test" }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArgType() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "invalid" }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_NullArgType() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = null }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_EmptyArgType() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "" }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_OptionalBeforeRequired() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "number", Optional = true },
                    new() { Type = "number", Optional = false }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArgAfterArray() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Arguments = [new() { Type = "number" }], Min = 1, Max = 5 },
                    new() { Type = "number" }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArrayLimits_MaxLessThanMin() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = 5, Max = 1, Arguments = [new() { Type = "number" }] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArrayLimits_NullMin() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = null, Max = 5, Arguments = [new() { Type = "number" }] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArrayLimits_NullMax() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = 1, Max = null, Arguments = [new() { Type = "number" }] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArrayLimits_NegativeMin() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = -1, Max = 5, Arguments = [new() { Type = "number" }] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_InvalidArrayLimits_NegativeMax() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = 0, Max = -1, Arguments = [new() { Type = "number" }] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArrayWithNoSubArguments() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = 1, Max = 5, Arguments = null }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArrayWithEmptySubArguments() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() { Type = "array", Min = 1, Max = 5, Arguments = [] }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArrayWithInvalidSubArgType() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() {
                        Type = "array",
                        Min = 1,
                        Max = 5,
                        Arguments = [
                            new() { Type = "invalid" }
                        ]
                    }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArrayWithNullSubArgType() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() {
                        Type = "array",
                        Min = 1,
                        Max = 5,
                        Arguments = [
                            new() { Type = null }
                        ]
                    }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckConfig_ArrayWithNestedArray() {
        var functions = new List<CalcFunction> {
            new() {
                Name = "test",
                Arguments = [
                    new() {
                        Type = "array",
                        Min = 1,
                        Max = 5,
                        Arguments = [
                            new() { Type = "array" }
                        ]
                    }
                ]
            }
        };
        AssertThrows<ConfigException>(() => CalcFunctionsHelper.CheckFunctionsConfiguration(functions));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_CountMismatch_Less() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number", Optional = false },
                new() { Type = "number", Optional = false }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 1 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_CountMismatch_More() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number", Optional = false }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 1 });
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 2 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_TypeMismatch_Number() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number" }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.String, Svalue = "not a number" });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_TypeMismatch_String() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "string" }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 42 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_TypeMismatch_Date() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "date" }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 42 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_TypeMismatch_Boolean() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "boolean" }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 42 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_TooFewElements() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array",
                    Min = 2,
                    Max = 5,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 1 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_ObsoleteArguments() {
        var function = new CalcFunction {
            Name = "calculateSingleThing",
            Arguments = [
                new() { Type = "string" },
                new() { Type = "number" },
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 5,
                    Arguments = [
                        new() { Type = "number" },
                        new() { Type = "string" }
                    ]
                }
            ]
        };
        var args = new Value[] {
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 1 },
            new() { DataType = DataType.Number, Nvalue = 1 },
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 2 } // Obsolete arg
        };

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_ArrayArgument_TypeMismatch() {
        var function = new CalcFunction {
            Name = "calculateSingleThing",
            Arguments = [
                new() { Type = "string" },
                new() { Type = "number" },
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 5,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new Value[] {
            new() { DataType = DataType.String, Svalue = "test" },
            new() { DataType = DataType.Number, Nvalue = 1 },
            new() { DataType = DataType.String, Svalue = "not a number" }
        };

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_MaxLessThanMin_Throws() {
        // This tests the internal validation: maxArgCount < minArgCount
        // Create a scenario where the array has Max=0 and Min=1 (invalid config)
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 0,  // Invalid: max < min
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var stack = new Stack<Value>();
        stack.Push(new Value { DataType = DataType.Number, Nvalue = 1 });

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, stack.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_GetMaxArgCount_NoArguments() {
        var result = CalcFunctionsHelper.GetMaxArgCount(null);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Test_GetMaxArgCount_SimpleArguments() {
        var args = new List<CalcFunctionArgument> {
            new() { Type = "number" },
            new() { Type = "string" }
        };
        var result = CalcFunctionsHelper.GetMaxArgCount(args);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Test_GetMaxArgCount_WithArray() {
        var args = new List<CalcFunctionArgument> {
            new() { Type = "number" },
            new() {
                Type = "array",
                Min = 1,
                Max = 5,
                Arguments = [
                    new() { Type = "number" }
                ]
            }
        };
        var result = CalcFunctionsHelper.GetMaxArgCount(args);
        Assert.AreEqual(6, result); // 1 + 5
    }

    [TestMethod]
    public void Test_GetMaxArgCount_WithArrayMultipleSubArgs() {
        var args = new List<CalcFunctionArgument> {
            new() {
                Type = "array",
                Min = 1,
                Max = 3,
                Arguments = [
                    new() { Type = "number" },
                    new() { Type = "string" }
                ]
            }
        };
        var result = CalcFunctionsHelper.GetMaxArgCount(args);
        Assert.AreEqual(6, result); // 3 * 2
    }

    [TestMethod]
    public void Test_GetMaxArgCount_WithArrayNullArguments() {
        var args = new List<CalcFunctionArgument> {
            new() {
                Type = "array",
                Min = 1,
                Max = 5,
                Arguments = null
            }
        };
        var result = CalcFunctionsHelper.GetMaxArgCount(args);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Test_GetMinArgCount_NoArguments() {
        var result = CalcFunctionsHelper.GetMinArgCount(null);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Test_GetMinArgCount_SimpleArguments() {
        var args = new List<CalcFunctionArgument> {
            new() { Type = "number", Optional = false },
            new() { Type = "string", Optional = false }
        };
        var result = CalcFunctionsHelper.GetMinArgCount(args);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Test_GetMinArgCount_WithOptional() {
        var args = new List<CalcFunctionArgument> {
            new() { Type = "number", Optional = false },
            new() { Type = "string", Optional = true }
        };
        var result = CalcFunctionsHelper.GetMinArgCount(args);
        Assert.AreEqual(1, result); // Stops at first optional
    }

    [TestMethod]
    public void Test_GetMinArgCount_WithArray() {
        var args = new List<CalcFunctionArgument> {
            new() { Type = "number", Optional = false },
            new() {
                Type = "array",
                Min = 2,
                Max = 5,
                Arguments = [
                    new() { Type = "number" }
                ]
            }
        };
        var result = CalcFunctionsHelper.GetMinArgCount(args);
        Assert.AreEqual(3, result); // 1 + 2
    }

    [TestMethod]
    public void Test_GetMinArgCount_WithArrayMultipleSubArgs() {
        var args = new List<CalcFunctionArgument> {
            new() {
                Type = "array",
                Min = 2,
                Max = 5,
                Arguments = [
                    new() { Type = "number" },
                    new() { Type = "string" }
                ]
            }
        };
        var result = CalcFunctionsHelper.GetMinArgCount(args);
        Assert.AreEqual(4, result); // 2 * 2
    }

    [TestMethod]
    public void Test_GetMinArgCount_WithArrayNullArguments() {
        var args = new List<CalcFunctionArgument> {
            new() {
                Type = "array",
                Min = 2,
                Max = 5,
                Arguments = null
            }
        };
        var result = CalcFunctionsHelper.GetMinArgCount(args);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_MissingRequiredArg() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number", Optional = false },
                new() { Type = "string", Optional = false }
            ]
        };

        var args = new[] {
            new Value { DataType = DataType.Number, Nvalue = 42 }
        };

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_ArrayZeroArgumentsNotOptional() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 5,
                    Optional = false,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };

        var args = Array.Empty<Value>();

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_ObsoleteArgs_MinEqualsMax() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 1,  // Min == Max
                    Arguments = [
                        new() { Type = "number" },
                        new() { Type = "string" }
                    ]
                }
            ]
        };

        // Provide 3 arguments, but we need chunks of 2
        var args = new[] {
            new Value { DataType = DataType.Number, Nvalue = 1 },
            new Value { DataType = DataType.String, Svalue = "test" },
            new Value { DataType = DataType.Number, Nvalue = 2 }
        };

        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_NullRepArgType() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array",
                    Min = 1,
                    Max = 5,
                    Arguments = [
                        new() { Type = null }  // Null type, should default to "string"
                    ]
                }
            ]
        };
        var args = new List<Value> {
            new() { DataType = DataType.String, Svalue = "test" }
        };
        CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test");
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_ArgsConfig_Null() {
        var function = new CalcFunction { Name = "test", Arguments = null };
        var args = new List<Value>();
        CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test");
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_Array_NullArguments() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "array", Arguments = null },
                new() { Type = "number" }
            ]
        };
        var args = new List<Value> {
            new() { DataType = DataType.Number, Nvalue = 1 },
            new() { DataType = DataType.Number, Nvalue = 2 }
        };
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_ObsoleteArgs_MinNotEqualsMax() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array", Min = 1, Max = 2,
                    Arguments = [
                        new() { Type = "number" },
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new List<Value> {
            new() { DataType = DataType.Number, Nvalue = 1 },
            new() { DataType = DataType.Number, Nvalue = 2 },
            new() { DataType = DataType.Number, Nvalue = 3 }
        };
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_RegularArgTypeMismatch() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number" }
            ]
        };
        var args = new List<Value> {
            new() { DataType = DataType.String, Svalue = "not a number" }
        };
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_InvalidCount_Direct() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() { Type = "number", Optional = false }
            ]
        };
        var args = Array.Empty<Value>();
        // Zero arguments provided, min is 1
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckFunctionArguments_TooManyArgs_Direct() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = []
        };
        var args = new[] { new Value { DataType = DataType.Number, Nvalue = 1 } };
        // One argument provided, max is 0
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckFunctionArguments(function, args, "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_RepetitionTypeMismatch() {
        var function = new CalcFunction {
            Name = "test",
            Arguments = [
                new() {
                    Type = "array", Min = 1, Max = 1,
                    Arguments = [
                        new() { Type = "number" }
                    ]
                }
            ]
        };
        var args = new List<Value> {
            new() { DataType = DataType.String, Svalue = "not a number" }
        };
        AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test"));
    }

    [TestMethod]
    public void Test_CheckArgTypes_Internal_NullableMinMax_Coverage() {
        // Test all branches of int? != int? in the obsolete arguments check

        var configs = new[] {
            new { Min = (int?)1, Max = (int?)null, ExpectedBody = true },
            new { Min = (int?)null, Max = (int?)1, ExpectedBody = true },
            new { Min = (int?)null, Max = (int?)null, ExpectedBody = false }
        };

        foreach (var cfg in configs) {
            var function = new CalcFunction {
                Name = "test",
                Arguments = [
                    new() {
                        Type = "array",
                        Min = cfg.Min,
                        Max = cfg.Max,
                        Arguments = [
                            new() { Type = "number" },
                            new() { Type = "number" }
                        ]
                    }
                ]
            };

            // chunk size 2. argumentsLeft 1 -> obsoleteArguments 1.
            var args = new List<Value> {
                new() { DataType = DataType.Number, Nvalue = 1 }
            };

            // We just want to see it hit the branch and throw the exception
            AssertThrows<FunctionException>(() => CalcFunctionsHelper.CheckArgTypes(function, args.ToArray(), "test"));
        }
    }
}
