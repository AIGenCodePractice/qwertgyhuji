using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Explicit MSTest attribute/API demonstrations required by the portfolio rubric.
/// These tests deliberately use methods that throw ArgumentException for invalid input.
/// </summary>
[TestClass]
public class MSTestExceptionAndIgnoreDemonstrationTests
{
    private readonly InterestCalculator _calculator = new();

    // -----------------------------------------------------------------
    // [ExpectedException] demonstrations (3 distinct examples)
    // -----------------------------------------------------------------

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ExpectedException_SimpleInterest_ZeroPrincipal()
    {
        _calculator.SimpleInterest(0m, 0.05m, 12);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ExpectedException_CompoundInterest_NegativeRate()
    {
        _calculator.CompoundInterest(1000m, -0.01m, 12, 12);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ExpectedException_DailyInterest_ZeroDays()
    {
        _calculator.DailyInterest(1000m, 0.05m, 0);
    }

    // -----------------------------------------------------------------
    // Assert.ThrowsException<T> demonstrations (3 distinct examples)
    // -----------------------------------------------------------------

    [TestMethod]
    public void ThrowsException_EffectiveAnnualRate_NegativeRate()
    {
        var exception = Assert.ThrowsException<ArgumentException>(
            () => _calculator.EffectiveAnnualRate(-0.01m, 12));

        StringAssert.Contains(exception.Message, "Rate cannot be negative");
    }

    [TestMethod]
    public void ThrowsException_CompoundInterest_ZeroFrequency()
    {
        var exception = Assert.ThrowsException<ArgumentException>(
            () => _calculator.CompoundInterest(1000m, 0.05m, 12, 0));

        StringAssert.Contains(exception.Message, "Compounding frequency must be positive");
    }

    [TestMethod]
    public void ThrowsException_FutureValue_ZeroMonths()
    {
        var exception = Assert.ThrowsException<ArgumentException>(
            () => _calculator.FutureValue(1000m, 0.05m, 0));

        StringAssert.Contains(exception.Message, "Months must be positive");
    }

    // -----------------------------------------------------------------
    // [Ignore] demonstration
    // -----------------------------------------------------------------

    [TestMethod]
    [Ignore("Documented demo of MSTest Ignore: this exploratory precision test is intentionally excluded because exact floating-point behaviour is not a stable acceptance criterion; rounded public results are tested elsewhere.")]
    public void Ignored_ExploratoryCompoundInterest_PrecisionExperiment()
    {
        var result = _calculator.CompoundInterest(1234.56m, 0.0735m, 17, 365);
        Assert.AreEqual(123.456789m, result);
    }
}
