using BankCore.Core.Services;
using FluentAssertions;
using Xunit;

namespace BankCore.Tests.xUnit;

public class InterestBranchCoverageTests
{
    private readonly InterestCalculator _calc = new();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SimpleInterest_InvalidPrincipalBranches_Throw(decimal principal)
        => Assert.Throws<ArgumentException>(() => _calc.SimpleInterest(principal, 0.05m, 12));

    [Theory]
    [InlineData(-0.01)]
    public void CompoundAndDaily_NegativeRateBranches_Throw(decimal rate)
    {
        Assert.Throws<ArgumentException>(() => _calc.CompoundInterest(1000m, rate, 12, 12));
        Assert.Throws<ArgumentException>(() => _calc.DailyInterest(1000m, rate, 30));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DailyInterest_InvalidDayBranches_Throw(int days)
        => Assert.Throws<ArgumentException>(() => _calc.DailyInterest(1000m, 0.05m, days));

    [Theory]
    [InlineData(-0.01, 12)]
    [InlineData(0.05, 0)]
    [InlineData(0.05, -1)]
    public void EffectiveAnnualRate_InvalidBranches_Throw(decimal rate, int frequency)
        => Assert.Throws<ArgumentException>(() => _calc.EffectiveAnnualRate(rate, frequency));

    [Fact]
    public void EffectiveAnnualRate_ValidBranch_ReturnsExpectedRoundedValue()
        => _calc.EffectiveAnnualRate(0.12m, 12).Should().Be(0.126825m);

    [Theory]
    [InlineData(true, 1104.71)]
    [InlineData(false, 1100.00)]
    public void FutureValue_CoversCompoundAndSimpleBranches(bool compound, decimal expected)
    {
        var result = _calc.FutureValue(1000m, 0.10m, 12, compound);
        result.Should().BeApproximately(expected, 0.01m);
    }

    [Theory]
    [InlineData(0, 0.05, 12)]
    [InlineData(1000, -0.01, 12)]
    [InlineData(1000, 0.05, 0)]
    public void FutureValue_InvalidValidationBranches_Throw(decimal principal, decimal rate, int months)
        => Assert.Throws<ArgumentException>(() => _calc.FutureValue(principal, rate, months));
}
