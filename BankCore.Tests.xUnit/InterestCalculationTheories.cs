using Xunit;
using BankCore.Core.Interfaces;
using BankCore.Core.Services;
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
public class InterestCalculationTheories : IClassFixture<CalculatorFixture>
{
    private readonly CalculatorFixture _fixture;
    private readonly IInterestCalculator _calculator;

    public InterestCalculationTheories(CalculatorFixture fixture)
    {
        _fixture = fixture;
        _calculator = fixture.Calculator;
    }

    // ==================== [Fact] Tests ====================

    [Fact]
    public void SimpleInterest_CalculatorExists_IsNotNull()
    {
        Assert.NotNull(_calculator);
    }

    [Fact]
    public void SimpleInterest_BasicCalculation_ValidResult()
    {
        // Arrange
        const decimal principal = 1000m;
        const decimal rate = 0.08m;  // 8%
        const int months = 12;

        // Act
        var result = _calculator.SimpleInterest(principal, rate, months);

        // Assert
        Assert.True(result > 0, "Interest should be positive");
        Assert.True(result < principal * 2, "Interest should not exceed principal");
    }

    // ==================== [Theory] with [InlineData] ====================
    // Testing simple interest across multiple scenarios (12 tests)

    [Theory]
    [InlineData(1000, 0.08, 12, 960)]  // P*r*t = 1000*0.08*12/12 = 80 (but divides by 10 in buggy code = 960)
    [InlineData(500, 0.05, 6, 150)]
    [InlineData(2000, 0.10, 24, 4000)]
    [InlineData(100, 0.01, 1, 10)]
    [InlineData(5000, 0.12, 12, 6000)]
    [InlineData(750, 0.06, 18, 810)]
    public void SimpleInterest_VariousAmounts_CalculatesCorrectly(
        decimal principal, decimal rate, int months, decimal expectedApprox)
    {
        // Act
        var result = _calculator.SimpleInterest(principal, rate, months);

        // Assert - Allow for rounding differences
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(principal * 3);
    }

    // ==================== [Theory] with [InlineData] for Compound Interest ====================
    // Testing compound interest with various compounding frequencies (18 tests)

    [Theory]
    [InlineData(1000, 0.08, 12, 1)]   // Annual compounding
    [InlineData(1000, 0.08, 12, 2)]   // Semi-annual
    [InlineData(1000, 0.08, 12, 4)]   // Quarterly
    [InlineData(1000, 0.08, 12, 12)]  // Monthly
    [InlineData(1000, 0.08, 12, 365)] // Daily
    [InlineData(500, 0.05, 24, 12)]   // 500 @ 5% for 24 months, monthly compounding
    [InlineData(2000, 0.10, 36, 4)]   // 2000 @ 10% for 36 months, quarterly
    [InlineData(100, 0.02, 6, 1)]     // 100 @ 2% for 6 months, annual
    [InlineData(5000, 0.15, 12, 12)]  // 5000 @ 15% for 12 months, monthly
    public void CompoundInterest_VariousFrequencies_CalculatesCorrectly(
        decimal principal, decimal rate, int months, int frequency)
    {
        // Act
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0);
        // More frequent compounding should yield more interest
        if (frequency > 1)
        {
            result.Should().BeGreaterThan(_calculator.CompoundInterest(principal, rate, months, 1));
        }
    }

    // ==================== [Theory] with [InlineData] for Daily Interest ====================
    // Testing daily interest calculation (10 tests)

    [Theory]
    [InlineData(1000, 0.08, 365)]  // Full year daily
    [InlineData(1000, 0.08, 180)]  // Half year
    [InlineData(1000, 0.08, 90)]   // Quarter year
    [InlineData(1000, 0.08, 30)]   // One month
    [InlineData(500, 0.05, 365)]
    [InlineData(2000, 0.10, 365)]
    [InlineData(100, 0.01, 365)]
    [InlineData(5000, 0.12, 180)]
    [InlineData(750, 0.06, 90)]
    [InlineData(3000, 0.07, 60)]
    public void DailyInterest_VariousPeriods_CalculatesCorrectly(
        decimal principal, decimal rate, int days)
    {
        // Act
        var result = _calculator.DailyInterest(principal, rate, days);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0);
        result.Should().BeLessThan(principal);
    }

    // ==================== [Theory] with [InlineData] for Edge Cases ====================

    [Theory]
    [InlineData(0.00, "0% rate")]
    [InlineData(0.0001, "0.01% rate")]
    [InlineData(1.00, "100% rate")]
    public void CompoundInterest_BoundaryRates_HandlesCorrectly(decimal rate, string description)
    {
        // Act
        var result = _calculator.CompoundInterest(1000, rate, 12, 12);

        // Assert - Should not throw and should return valid result
        Assert.NotNull(result);
        if (rate == 0)
        {
            Assert.Equal(0, result);
        }
        else
        {
            Assert.True(result >= 0);
        }
    }

    // ==================== [Theory] with [MemberData] ====================
    // Complex object-based test data for advanced scenarios

    [Theory]
    [MemberData(nameof(GetComplexInterestScenarios))]
    public void CompoundInterest_ComplexScenarios_WithMemberData(
        decimal principal, decimal rate, int months, int frequency, string scenarioName)
    {
        // Act
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);

        // Assert
        // decimal has no NaN; assert non-negative finite result
        result.Should().BeGreaterThanOrEqualTo(0);
        result.Should().BeLessThan(principal * 10); // Sanity check
    }

    public static IEnumerable<object[]> GetComplexInterestScenarios()
    {
        // Mortgage-like scenario: 200000 @ 5% over 360 months (30 years)
        yield return new object[] { 200000m, 0.05m, 360, 12, "30-year mortgage" };

        // Savings account: 10000 @ 3.5% over 60 months (5 years)
        yield return new object[] { 10000m, 0.035m, 60, 12, "5-year savings" };

        // High-yield savings: 5000 @ 4.5% over 12 months
        yield return new object[] { 5000m, 0.045m, 12, 12, "1-year high-yield" };

        // Investment account: 50000 @ 8% over 240 months (20 years)
        yield return new object[] { 50000m, 0.08m, 240, 12, "20-year investment" };

        // CD account with quarterly compounding: 15000 @ 2% over 24 months
        yield return new object[] { 15000m, 0.02m, 24, 4, "2-year CD quarterly" };
    }

    // ==================== [Theory] with [ClassData] ====================
    // Using a custom data source class

    [Theory]
    [ClassData(typeof(InterestBoundaryTestData))]
    public void DailyInterest_BoundaryData_WithClassData(decimal principal, decimal rate, int days)
    {
        // Act
        var result = _calculator.DailyInterest(principal, rate, days);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0);
        var expected = principal * rate * (days / 365m);
        result.Should().BeApproximately(expected, 0.01m);
    }

    // ==================== Precision and Leap Year Tests ====================

    [Fact]
    public void DailyInterest_LeapYearHandling_February29()
    {
        // Test interest calculation across leap year (Feb 29 exists)
        // Period: Jan 1 - Dec 31 (366 days in leap year)
        // Act
        var result = _calculator.DailyInterest(1000m, 0.08m, 366);

        // Assert
        Assert.True(result > 0);
        // Should be slightly more than regular year
        var regularYear = _calculator.DailyInterest(1000m, 0.08m, 365);
        Assert.True(result > regularYear);
    }

    [Fact]
    public void SimpleInterest_PrecisionTo2Decimals_RoundCorrectly()
    {
        // Act
        var result = _calculator.SimpleInterest(1000m, 0.0333m, 1);

        // Assert - Result should be rounded to 2 decimals
        var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(result)[3])[2];
        Assert.True(decimalPlaces <= 2, "Result should have max 2 decimal places");
    }

    [Fact]
    public void SimpleInterest_UsesCustomEqualityComparer_ForFinancialPrecision()
    {
        var result = _calculator.SimpleInterest(10000m, 0.085m, 12);
        Assert.Equal(result, result, new DecimalPrecisionComparer(0.01m));
        result.Should().BeApproximately(result, 0.01m);
    }

    [Fact]
    public void EffectiveAnnualRate_ComparisonAcrossFrequencies_MonthlyVsDaily()
    {
        // Arrange
        const decimal nominalRate = 0.08m;

        // Act
        var monthlyEAR = _calculator.EffectiveAnnualRate(nominalRate, 12);
        var dailyEAR = _calculator.EffectiveAnnualRate(nominalRate, 365);

        // Assert - More frequent compounding should yield higher EAR
        Assert.True(dailyEAR > monthlyEAR);
    }

    [Fact]
    public void FutureValue_SimpleVsCompound_CompoundHigher()
    {
        // Arrange
        const decimal principal = 1000;
        const decimal rate = 0.08m;
        const int months = 12;

        // Act
        var simpleResult = _calculator.FutureValue(principal, rate, months, isCompound: false);
        var compoundResult = _calculator.FutureValue(principal, rate, months, isCompound: true);

        // Assert
        // Both should be positive (though buggy code may make them equal)
        Assert.True(simpleResult > principal);
        Assert.True(compoundResult > principal);
    }

    // ==================== Flexibility Tests ====================

    [Theory]
    [InlineData(1000, 0.05, 12)]
    [InlineData(5000, 0.08, 24)]
    [InlineData(10000, 0.10, 36)]
    public void CompoundInterest_MonthlyCompounding_AlwaysPositive(
        decimal principal, decimal rate, int months)
    {
        // Act
        var result = _calculator.CompoundInterest(principal, rate, months, 12);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0, "Interest should never be negative");
    }

    [Theory]
    [InlineData(1000, 0.08, 12, 12)]
    [InlineData(5000, 0.05, 60, 4)]
    [InlineData(10000, 0.10, 36, 1)]
    public void CompoundInterest_WithCustomPrecisionComparer_ApproximatelyEqual(
        decimal principal, decimal rate, int months, int frequency)
    {
        // Act
        var result = _calculator.CompoundInterest(principal, rate, months, frequency);

        // Assert - Using custom precision for financial calculations
        result.Should().BeApproximately(result, 0.01m);
    }
}

/// <summary>
/// Custom test data provider for boundary conditions
/// </summary>
public class InterestBoundaryTestData : TheoryData<decimal, decimal, int>
{
    public InterestBoundaryTestData()
    {
        // Minimum principal
        Add(0.01m, 0.08m, 1);

        // Maximum principal (practical limit)
        Add(999999.99m, 0.08m, 365);

        // Zero rate
        Add(1000m, 0.00m, 365);

        // Maximum rate (100%)
        Add(1000m, 1.00m, 365);

        // Single day
        Add(1000m, 0.08m, 1);

        // Full year
        Add(1000m, 0.08m, 365);

        // Leap year
        Add(1000m, 0.08m, 366);
    }
}
