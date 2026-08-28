using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Explicit MSTest 4 exception and Ignore demonstrations.
/// MSTest 4 removed ExpectedExceptionAttribute and Assert.ThrowsException&lt;T&gt;,
/// so the current supported Assert.Throws&lt;T&gt; API is used for the six exception paths.
/// </summary>
[TestClass]
public class MSTestExceptionAndIgnoreDemonstrationTests
{
    private readonly InterestCalculator _calculator = new();

    // -----------------------------------------------------------------
    // Exception assertion demonstrations (six distinct invalid paths)
    // -----------------------------------------------------------------

    [TestMethod]
    public void Throws_SimpleInterest_ZeroPrincipal()
    {
        Assert.Throws<ArgumentException>(() =>
            _calculator.SimpleInterest(0m, 0.05m, 12));
    }

    [TestMethod]
    public void Throws_CompoundInterest_NegativeRate()
    {
        Assert.Throws<ArgumentException>(() =>
            _calculator.CompoundInterest(1000m, -0.01m, 12, 12));
    }

    [TestMethod]
    public void Throws_DailyInterest_ZeroDays()
    {
        Assert.Throws<ArgumentException>(() =>
            _calculator.DailyInterest(1000m, 0.05m, 0));
    }

    [TestMethod]
    public void Throws_EffectiveAnnualRate_NegativeRate()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.EffectiveAnnualRate(-0.01m, 12));

        StringAssert.Contains(exception.Message, "Rate cannot be negative");
    }

    [TestMethod]
    public void Throws_CompoundInterest_ZeroFrequency()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.CompoundInterest(1000m, 0.05m, 12, 0));

        StringAssert.Contains(exception.Message, "Compounding frequency must be positive");
    }

    [TestMethod]
    public void Throws_FutureValue_ZeroMonths()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            _calculator.FutureValue(1000m, 0.05m, 0));

        StringAssert.Contains(exception.Message, "Months must be positive");
    }

    // -----------------------------------------------------------------
    // [Ignore] demonstration with documented reason
    // -----------------------------------------------------------------

    [TestMethod]
    [Ignore("Documented MSTest Ignore demonstration: this exploratory precision test is intentionally excluded because exact floating-point behaviour is not a stable acceptance criterion; rounded public results are tested elsewhere.")]
    public void Ignored_ExploratoryCompoundInterest_PrecisionExperiment()
    {
        var result = _calculator.CompoundInterest(1234.56m, 0.0735m, 17, 365);
        Assert.AreEqual(123.456789m, result);
    }
}
