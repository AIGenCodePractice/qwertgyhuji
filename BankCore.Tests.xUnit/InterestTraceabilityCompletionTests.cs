using BankCore.Core.Services;
using FluentAssertions;
using Xunit;

namespace BankCore.Tests.xUnit;

/// <summary>
/// Final strict traceability additions for the Interest module.
/// These tests close previously documented gaps between the written test design
/// and executable test evidence.
/// </summary>
public class InterestTraceabilityCompletionTests
{
    private readonly InterestCalculator _calculator = new();

    /// <summary>
    /// TC-INT-007 — explicit boundary test for a zero annual interest rate.
    /// A valid positive principal and period must calculate zero interest.
    /// </summary>
    [Fact]
    public void TC_INT_007_SimpleInterest_ZeroRate_ReturnsZero()
    {
        var result = _calculator.SimpleInterest(10_000m, 0m, 12);

        result.Should().Be(0m);
    }

    /// <summary>
    /// INT-POS-06 / TC-INT-022 — additional positive scenario required by the
    /// written Interest-module minimum. Exercises FutureValue's successful
    /// non-compound branch using a full twelve-month period.
    /// </summary>
    [Fact]
    public void TC_INT_022_INT_POS_06_FutureValue_SimpleInterestBranch_ReturnsExpectedValue()
    {
        var result = _calculator.FutureValue(
            principal: 10_000m,
            annualRate: 0.10m,
            months: 12,
            isCompound: false);

        result.Should().Be(11_000m);
    }
}
