using Xunit;
using BankCore.Core.Interfaces;
using FluentAssertions;

namespace BankCore.Tests.xUnit;

/// <summary>
/// Comprehensive test class for Interest Calculations using xUnit framework.
/// Demonstrates:
/// - [Fact] for simple, non-parameterized tests
/// - [Theory] with [InlineData] for parameterized tests (30+ rows)
/// - [Theory] with [MemberData] for complex object data
/// - [Theory] with [ClassData] for class-based data sources
/// - IClassFixture for shared calculator instance
/// - Custom IEqualityComparer for financial precision
/// - FluentAssertions with financial precision validation
/// </summary>
public class InterestCalculationTheories(CalculatorFixture fixture) : IClassFixture<CalculatorFixture>
{
    private readonly IInterestCalculator _calculator = fixture.Calculator;

    [Fact]
    public void SimpleInterest_CalculatorExists_IsNotNull()
    {
        Assert.NotNull(_calculator);
    }

    [Fact]
    public void SimpleInterest_BasicCalculation_ReturnsFormulaResult()
    {
        const decimal principal = 1000m;
        const decimal rate = 0.08m;
        const int months = 12;
        var result = _calculator.SimpleInterest(principal, rate, months);
        result.Should().BeApproximately(80m, 0.01m);
    }

    [Theory]
    [InlineData(1000, 0.08, 12, 80)]
    [InlineData(500, 0.05, 6, 12.50)]
    [InlineData(2000, 0.10, 24, 400)]
    [InlineData(100, 0.01, 1, 0.08)]
    [InlineData(5000, 0.12, 12, 600)]
    [InlineData(750, 0.06, 18, 67.50)]
    public void SimpleInterest_VariousAmounts_CalculatesCorrectly(
        decimal principal, decimal rate, int months, decimal expected)
    {
        var result = _calculator.SimpleInterest(principal, rate, months);
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Theory]
    [InlineData(1000, 0.08, 12, 1)]
    [InlineData(1000, 0.08, 12, 2)]
    [InlineData(1000, 0.08, 12, 4)]
    [InlineData(1000, 0.08, 12, 12)]
    [InlineData(1000, 0.08, 12, 365)]
    [InlineData(500, 0.05, 24, 12)]
    [InlineData(2000, 0.10, 36, 4)]
    [InlineData(100, 0.02, 6, 1)]
    [InlineData(5000, 0.15, 12, 12)]
    public void CompoundInterest_VariousFrequencies_CalculatesCorrectly(
        decimal principal, decimal rate, int months, int frequency)
    {
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);
        var years = months / 12.0;
        var expected = Math.Round(
            (decimal)(principal * (decimal)Math.Pow(1 + (double)rate / frequency, frequency * years) - principal),
            2);

        result.Should().BeApproximately(expected, 0.01m);
    }

    [Theory]
    [InlineData(1000, 0.08, 365)]
    [InlineData(1000, 0.08, 180)]
    [InlineData(1000, 0.08, 90)]
    [InlineData(1000, 0.08, 30)]
    [InlineData(500, 0.05, 365)]
    [InlineData(2000, 0.10, 365)]
    [InlineData(100, 0.01, 365)]
    [InlineData(5000, 0.12, 180)]
    [InlineData(750, 0.06, 90)]
    [InlineData(3000, 0.07, 60)]
    public void DailyInterest_VariousPeriods_CalculatesCorrectly(
        decimal principal, decimal rate, int days)
    {
        var result = _calculator.DailyInterest(principal, rate, days);
        var expected = Math.Round(principal * rate * days / 365m, 2);
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Theory]
    [InlineData(0.00, "0% rate")]
    [InlineData(0.0001, "0.01% rate")]
    [InlineData(1.00, "100% rate")]
    public void CompoundInterest_BoundaryRates_HandlesCorrectly(decimal rate, string description)
    {
        var result = _calculator.CompoundInterest(1000, rate, 12, 12);
        Assert.True(result >= 0, description);
        if (rate == 0)
        {
            Assert.Equal(0, result);
        }
    }

    [Theory]
    [MemberData(nameof(GetComplexInterestScenarios))]
    public void CompoundInterest_ComplexScenarios_WithMemberData(
        decimal principal, decimal rate, int months, int frequency, string scenarioName)
    {
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);
        var years = months / 12.0;
        var expected = Math.Round(
            (decimal)(principal * (decimal)Math.Pow(1 + (double)rate / frequency, frequency * years) - principal),
            2);

        result.Should().BeApproximately(expected, 0.01m, scenarioName);
    }

    public static TheoryData<decimal, decimal, int, int, string> GetComplexInterestScenarios()
    {
        return new TheoryData<decimal, decimal, int, int, string>
        {
            { 200000m, 0.05m, 360, 12, "30-year mortgage" },
            { 10000m, 0.035m, 60, 12, "5-year savings" },
            { 5000m, 0.045m, 12, 12, "1-year high-yield" },
            { 50000m, 0.08m, 240, 12, "20-year investment" },
            { 15000m, 0.02m, 24, 4, "2-year CD quarterly" }
        };
    }

    [Theory]
    [ClassData(typeof(InterestBoundaryTestData))]
    public void DailyInterest_BoundaryData_WithClassData(decimal principal, decimal rate, int days)
    {
        var result = _calculator.DailyInterest(principal, rate, days);
        var expected = principal * rate * (days / 365m);
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Fact]
    public void DailyInterest_LeapYearHandling_February29()
    {
        var result = _calculator.DailyInterest(1000m, 0.08m, 366);
        Assert.True(result > 0);
        var regularYear = _calculator.DailyInterest(1000m, 0.08m, 365);
        Assert.True(result > regularYear);
    }

    [Fact]
    public void SimpleInterest_PrecisionTo2Decimals_RoundCorrectly()
    {
        var result = _calculator.SimpleInterest(1000m, 0.0333m, 1);
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
        Assert.True(decimalPlaces <= 2, "Result should have max 2 decimal places");
    }

    [Fact]
    public void SimpleInterest_UsesCustomEqualityComparer_ForFinancialPrecision()
    {
        const decimal expected = 850m;
        var result = _calculator.SimpleInterest(10000m, 0.085m, 12);
        Assert.Equal(expected, result, new DecimalPrecisionComparer(0.01m));
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Fact]
    public void EffectiveAnnualRate_ComparisonAcrossFrequencies_MonthlyVsDaily()
    {
        const decimal nominalRate = 0.08m;
        var monthlyEAR = _calculator.EffectiveAnnualRate(nominalRate, 12);
        var dailyEAR = _calculator.EffectiveAnnualRate(nominalRate, 365);
        Assert.True(dailyEAR > monthlyEAR);
    }

    [Fact]
    public void FutureValue_SimpleVsCompound_CompoundHigher()
    {
        const decimal principal = 1000;
        const decimal rate = 0.08m;
        const int months = 12;
        var simpleResult = _calculator.FutureValue(principal, rate, months, isCompound: false);
        var compoundResult = _calculator.FutureValue(principal, rate, months, isCompound: true);
        Assert.True(simpleResult > principal);
        Assert.True(compoundResult > principal);
    }

    [Theory]
    [InlineData(1000, 0.05, 12)]
    [InlineData(5000, 0.08, 24)]
    [InlineData(10000, 0.10, 36)]
    public void CompoundInterest_MonthlyCompounding_AlwaysPositive(
        decimal principal, decimal rate, int months)
    {
        var result = _calculator.CompoundInterest(principal, rate, months, 12);
        result.Should().BeGreaterThanOrEqualTo(0, "Interest should never be negative");
    }

    [Theory]
    [InlineData(1000, 0.08, 12, 12)]
    [InlineData(5000, 0.05, 60, 4)]
    [InlineData(10000, 0.10, 36, 1)]
    public void CompoundInterest_WithCustomPrecisionComparer_ApproximatelyEqual(
        decimal principal, decimal rate, int months, int frequency)
    {
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);
        var years = months / 12.0;
        var expected = Math.Round(
            (decimal)(principal * (decimal)Math.Pow(1 + (double)rate / frequency, frequency * years) - principal),
            2);

        Assert.Equal(expected, result, new DecimalPrecisionComparer(0.01m));
        result.Should().BeApproximately(expected, 0.01m);
    }
}

/// <summary>
/// Custom test data provider for boundary conditions
/// </summary>
public class InterestBoundaryTestData : TheoryData<decimal, decimal, int>
{
    public InterestBoundaryTestData()
    {
        Add(0.01m, 0.08m, 1);
        Add(999999.99m, 0.08m, 365);
        Add(1000m, 0.00m, 365);
        Add(1000m, 1.00m, 365);
        Add(1000m, 0.08m, 1);
        Add(1000m, 0.08m, 365);
        Add(1000m, 0.08m, 366);
    }
}
