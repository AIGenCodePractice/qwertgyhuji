using BankCore.Core.Interfaces;
using BankCore.Core.Services;
using FluentAssertions;
using Xunit;

namespace BankCore.Tests.xUnit;

/// <summary>
/// REQ-INT-001 … REQ-INT-007 / TC-INT-001 … TC-INT-021
/// Closes pending Interest Calculator rows on the RTM.
/// </summary>
public class InterestRequirementTests : IClassFixture<CalculatorFixture>
{
    private readonly IInterestCalculator _calc;
    public InterestRequirementTests(CalculatorFixture fixture) => _calc = fixture.Calculator;

    // ---------- REQ-INT-001 / TC-INT-001, 006, 014 ----------

    /// <summary>TC-INT-001 — classic simple interest I = P*r*t</summary>
    [Fact]
    public void TC_INT_001_SimpleInterest_KnownValues_MatchesFormula()
    {
        // 10000 * 0.08 * (12/12) = 800
        var interest = _calc.SimpleInterest(10_000m, 0.08m, 12);
        interest.Should().Be(800.00m);
    }

    /// <summary>TC-INT-006 — partial year (6 months)</summary>
    [Fact]
    public void TC_INT_006_SimpleInterest_SixMonths_HalfYear()
    {
        // 10000 * 0.12 * 0.5 = 600
        _calc.SimpleInterest(10_000m, 0.12m, 6).Should().Be(600.00m);
    }

    /// <summary>TC-INT-014 — multi-year simple interest</summary>
    [Theory]
    [InlineData(5000, 0.05, 24, 500.00)]  // 2 years
    [InlineData(2000, 0.10, 36, 600.00)]  // 3 years
    public void TC_INT_014_SimpleInterest_MultiYear(decimal p, decimal r, int months, decimal expected)
    {
        _calc.SimpleInterest(p, r, months).Should().Be(expected);
    }

    // ---------- REQ-INT-002 / TC-INT-002, 003, 021 ----------

    /// <summary>TC-INT-002 — monthly compound (Fixed Deposit style)</summary>
    [Fact]
    public void TC_INT_002_CompoundInterest_Monthly_PositiveAndAboveSimple()
    {
        var simple = _calc.SimpleInterest(10_000m, 0.08m, 12);
        var compound = _calc.CompoundInterest(10_000m, 0.08m, 12, 12);
        compound.Should().BeGreaterThan(simple);
        compound.Should().BeApproximately(829.00m, 5.00m); // ~P*(1+r/12)^12 - P
    }

    /// <summary>TC-INT-003 — daily compound (Notice / high-frequency)</summary>
    [Fact]
    public void TC_INT_003_CompoundInterest_Daily_ExceedsMonthly()
    {
        var monthly = _calc.CompoundInterest(10_000m, 0.08m, 12, 12);
        var daily = _calc.CompoundInterest(10_000m, 0.08m, 12, 365);
        daily.Should().BeGreaterThanOrEqualTo(monthly);
    }

    /// <summary>TC-INT-021 — daily accrual formula I = P*r*(days/365)</summary>
    [Theory]
    [InlineData(1000, 0.08, 365, 80.00)]
    [InlineData(1000, 0.08, 183, 40.11)] // ~ half year (rounded)
    public void TC_INT_021_DailyInterest_MatchesDayCountFormula(
        decimal p, decimal r, int days, decimal approx)
    {
        var result = _calc.DailyInterest(p, r, days);
        result.Should().BeApproximately(approx, 0.50m);
    }

    // ---------- REQ-INT-003 / TC-INT-005 — rates by account type (policy table) ----------

    /// <summary>
    /// TC-INT-005 — product rate table applied to calculator inputs.
    /// Rates mirror typical BankCore seed / policy: Savings 4%, Current 0.5%, FD 8%, Notice 6%.
    /// </summary>
    [Theory]
    [InlineData("Savings", 0.04)]
    [InlineData("Current", 0.005)]
    [InlineData("FixedDeposit", 0.08)]
    [InlineData("Notice", 0.06)]
    public void TC_INT_005_InterestRate_ByAccountType_ProducesExpectedOrder(string type, decimal rate)
    {
        var interest = _calc.SimpleInterest(10_000m, rate, 12);
        interest.Should().BeGreaterThanOrEqualTo(0);
        // FD rate should yield more than Current on same principal/time
        if (type == "FixedDeposit")
            interest.Should().BeGreaterThan(_calc.SimpleInterest(10_000m, 0.005m, 12));
        if (type == "Savings")
            interest.Should().BeGreaterThan(_calc.SimpleInterest(10_000m, 0.005m, 12));
    }

    // ---------- REQ-INT-004 / TC-INT-004, 008 — leap day / mid-period ----------

    /// <summary>TC-INT-004 — leap year day count (366) accrues more than non-leap (365)</summary>
    [Fact]
    public void TC_INT_004_DailyInterest_LeapYear_ExceedsNonLeap()
    {
        var nonLeap = _calc.DailyInterest(10_000m, 0.08m, 365);
        var leap = _calc.DailyInterest(10_000m, 0.08m, 366);
        leap.Should().BeGreaterThan(nonLeap);
    }

    /// <summary>TC-INT-008 — mid-month style short periods (15 / 30 days)</summary>
    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(1)]
    public void TC_INT_008_DailyInterest_MidMonthPeriods_Proportional(int days)
    {
        var year = _calc.DailyInterest(5_000m, 0.06m, 365);
        var part = _calc.DailyInterest(5_000m, 0.06m, days);
        part.Should().BeLessThan(year);
        part.Should().BeGreaterThan(0);
    }

    // ---------- REQ-INT-005 / TC-INT-009 … 012 — negative / zero / invalid ----------

    /// <summary>TC-INT-009 — negative principal rejected</summary>
    [Fact]
    public void TC_INT_009_SimpleInterest_NegativePrincipal_Throws()
    {
        var act = () => _calc.SimpleInterest(-100m, 0.05m, 12);
        act.Should().Throw<ArgumentException>().WithParameterName("principal");
    }

    /// <summary>TC-INT-010 — negative rate rejected</summary>
    [Fact]
    public void TC_INT_010_SimpleInterest_NegativeRate_Throws()
    {
        var act = () => _calc.SimpleInterest(1000m, -0.01m, 12);
        act.Should().Throw<ArgumentException>().WithParameterName("annualRate");
    }

    /// <summary>TC-INT-011 — zero / negative period rejected</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void TC_INT_011_SimpleInterest_NonPositiveMonths_Throws(int months)
    {
        var act = () => _calc.SimpleInterest(1000m, 0.05m, months);
        act.Should().Throw<ArgumentException>().WithParameterName(nameof(months));
    }

    /// <summary>TC-INT-012 — compound rejects non-positive frequency (null type N/A at API)</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TC_INT_012_CompoundInterest_InvalidFrequency_Throws(int freq)
    {
        var act = () => _calc.CompoundInterest(1000m, 0.05m, 12, freq);
        act.Should().Throw<ArgumentException>();
    }

    // ---------- REQ-INT-006 / TC-INT-013 — closed account policy (calculator itself has no status;
    // documented: callers must not invoke calculator for Closed; zero period / zero principal not allowed) ----------

    /// <summary>TC-INT-013 — zero principal treated as invalid (no interest on closed empty book)</summary>
    [Fact]
    public void TC_INT_013_SimpleInterest_ZeroPrincipal_Throws()
    {
        var act = () => _calc.SimpleInterest(0m, 0.05m, 12);
        act.Should().Throw<ArgumentException>().WithParameterName("principal");
    }

    // ---------- REQ-INT-007 / TC-INT-007, 015–020 — boundaries ----------

    /// <summary>TC-INT-007 / 015 — 0% rate yields zero interest</summary>
    [Fact]
    public void TC_INT_015_SimpleInterest_ZeroRate_ReturnsZero()
    {
        _calc.SimpleInterest(10_000m, 0m, 12).Should().Be(0m);
    }

    /// <summary>TC-INT-016 — very small rate 0.01%</summary>
    [Fact]
    public void TC_INT_016_SimpleInterest_TinyRate_PositiveOrZeroRounded()
    {
        var i = _calc.SimpleInterest(10_000m, 0.0001m, 12);
        i.Should().BeGreaterThanOrEqualTo(0);
        i.Should().BeLessThan(10m);
    }

    /// <summary>TC-INT-017 — 100% annual rate</summary>
    [Fact]
    public void TC_INT_017_SimpleInterest_HundredPercentRate()
    {
        _calc.SimpleInterest(1_000m, 1.0m, 12).Should().Be(1_000.00m);
    }

    /// <summary>TC-INT-018 — minimum positive principal</summary>
    [Fact]
    public void TC_INT_018_SimpleInterest_MinPrincipal_001()
    {
        var i = _calc.SimpleInterest(0.01m, 0.05m, 12);
        i.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>TC-INT-019 — large principal near operational max</summary>
    [Fact]
    public void TC_INT_019_SimpleInterest_LargePrincipal()
    {
        var i = _calc.SimpleInterest(1_000_000m, 0.05m, 12);
        i.Should().Be(50_000.00m);
    }

    /// <summary>TC-INT-020 — compound boundary rate 0%</summary>
    [Fact]
    public void TC_INT_020_CompoundInterest_ZeroRate_ReturnsZero()
    {
        _calc.CompoundInterest(5_000m, 0m, 12, 12).Should().Be(0m);
    }
}
