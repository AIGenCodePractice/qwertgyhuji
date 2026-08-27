using BankCore.Core.Interfaces;
using BankCore.Core.Services;

namespace BankCore.Tests.xUnit;

/// <summary>
/// Shared xUnit fixture (IClassFixture / ICollectionFixture) providing a reusable
/// InterestCalculator instance so theories and loan tests do not re-instantiate per test.
/// </summary>
public class CalculatorFixture : IDisposable
{
    public IInterestCalculator Calculator { get; } = new InterestCalculator();
    public InterestCalculator InterestCalculator => (InterestCalculator)Calculator;

    public void Dispose() { }
}

[CollectionDefinition("CalculatorCollection")]
public class CalculatorCollection : ICollectionFixture<CalculatorFixture>
{
}

/// <summary>
/// Custom IEqualityComparer for financial amounts (Phase 3 xUnit requirement).
/// Compares decimals within 0.01 (2 decimal places).
/// </summary>
public sealed class DecimalPrecisionComparer : IEqualityComparer<decimal>
{
    private readonly decimal _tolerance;

    public DecimalPrecisionComparer(decimal tolerance = 0.01m) => _tolerance = tolerance;

    public bool Equals(decimal x, decimal y) => Math.Abs(x - y) <= _tolerance;

    public int GetHashCode(decimal obj) => decimal.Round(obj, 2).GetHashCode();
}
