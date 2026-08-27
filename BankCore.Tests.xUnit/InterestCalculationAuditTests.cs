using BankCore.Core.Interfaces;
using BankCore.Core.Services;
using FluentAssertions;
using Xunit;

namespace BankCore.Tests.xUnit;

[Collection("CalculatorCollection")]
public class InterestCalculationAuditTests
{
    private readonly IInterestCalculator _calculator;

    public InterestCalculationAuditTests(CalculatorFixture fixture)
        => _calculator = fixture.Calculator;

    [Theory]
    [InlineData(1000, 0.08, 12, 80.00)]
    [InlineData(500, 0.05, 6, 12.50)]
    [InlineData(2000, 0.10, 24, 400.00)]
    [InlineData(100, 0.01, 1, 0.08)]
    [InlineData(5000, 0.12, 12, 600.00)]
    public void SimpleInterest_KnownInputs_ReturnExactInterest(decimal principal, decimal rate, int months, decimal expected)
        => _calculator.SimpleInterest(principal, rate, months).Should().BeApproximately(expected, 0.01m);

    [Fact]
    public void CompoundInterest_MonthlyAtEightPercent_ReturnsExpectedInterest()
        => _calculator.CompoundInterest(1000m, 0.08m, 12, 12).Should().BeApproximately(82.99m, 0.01m);

    [Fact]
    public void FutureValue_SimpleBranch_ReturnsPrincipalPlusSimpleInterest()
        => _calculator.FutureValue(1000m, 0.08m, 12, false).Should().Be(1080m);
}
