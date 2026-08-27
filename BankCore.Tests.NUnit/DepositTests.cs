using NUnit.Framework;
using Moq;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;

namespace BankCore.Tests.NUnit;

/// <summary>
/// Comprehensive test class for Transaction Deposits using NUnit framework.
/// Demonstrates:
/// - [TestCase] attributes with 3+ parameters
/// - [TestCaseSource] for complex data-driven tests
/// - [SetUp] and [TearDown] for per-test fixtures
/// - [OneTimeSetUp] and [OneTimeTearDown] for shared state
/// - [Category] attributes
/// - [Retry] for timing-sensitive operations
/// - [Timeout] for performance tests
/// - [Values] for combinatorial testing
/// - Assert.Multiple for multiple assertions
/// - Assert.That with constraint-based assertions
/// - Moq for mocking repositories
/// </summary>
[TestFixture]
public class DepositTests
{
    private Mock<IAccountRepository> _mockAccountRepo = null!;
    private Mock<ITransactionRepository> _mockTxnRepo = null!;
    private Mock<IValidationService> _mockValidator = null!;
    private Mock<IAuditService> _mockAudit = null!;
    private TransactionService _transactionService = null!;

    private Account _testAccount = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Expensive setup that runs once for the entire test class
        TestContext.WriteLine("OneTimeSetUp: Initializing test fixtures");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Cleanup that runs once after all tests complete
        TestContext.WriteLine("OneTimeTearDown: Cleaning up shared resources");
    }

    [SetUp]
    public void Setup()
    {
        // Reinitialize mocks before each test
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockTxnRepo = new Mock<ITransactionRepository>();
        _mockValidator = new Mock<IValidationService>();
        _mockAudit = new Mock<IAuditService>();

        // Configure default validator behavior
        _mockValidator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

        // Create test account
        _testAccount = new Account
        {
            Id = 1,
            AccountNumber = "BC1000000001",
            OwnerName = "Test Owner",
            Type = AccountType.Savings,
            Status = AccountStatus.Active,
            Balance = 5000m,
            DailyWithdrawalLimit = 10000m,
            DailyWithdrawnToday = 0m,
            DateOpened = DateTime.UtcNow,
            BranchCode = "BRANCH001"
        };

        _mockAccountRepo.Setup(r => r.GetById(1)).Returns(_testAccount);

        _transactionService = new TransactionService(
            _mockAccountRepo.Object,
            _mockTxnRepo.Object,
            _mockValidator.Object,
            _mockAudit.Object);
    }

    [TearDown]
    public void Teardown()
    {
        // NUnit recreates fixture state via SetUp; no nulling required.
    }

    [Test]
    [Category("Critical")]
    public void Deposit_WithValidAmount_ReturnsSuccess()
    {
        // Arrange
        const decimal depositAmount = 1000m;

        // Act
        var result = _transactionService.Deposit(1, depositAmount, "Regular deposit", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Amount, Is.EqualTo(depositAmount));
    }

    [Test]
    [Category("Critical")]
    [TestCase(100, "R100 deposit")]
    [TestCase(500, "R500 deposit")]
    [TestCase(1000, "R1000 deposit")]
    [TestCase(5000, "R5000 deposit")]
    public void Deposit_WithVariousAmounts_UpdatesAccountBalance(decimal amount, string description)
    {
        // Arrange
        decimal balanceBefore = _testAccount.Balance;

        // Act
        var result = _transactionService.Deposit(1, amount, description, "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.Amount, Is.EqualTo(amount));
        // Balance should increase
        _mockAccountRepo.Verify(r => r.Update(It.Is<Account>(a => a.Balance == balanceBefore + amount)), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_ToInactiveAccount_ReturnsFailed()
    {
        // Arrange
        _testAccount.Status = AccountStatus.Dormant;
        _mockAccountRepo.Setup(r => r.GetById(1)).Returns(_testAccount);

        // Act
        var result = _transactionService.Deposit(1, 500m, "Test", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("Active"));
    }

    [Test]
    [Category("Critical")]
    public void Deposit_NegativeAmount_ReturnsFailed()
    {
        // Act
        var result = _transactionService.Deposit(1, -100m, "Negative deposit", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("greater than zero"));
    }

    [Test]
    [Category("Critical")]
    public void Deposit_ZeroAmount_ReturnsFailed()
    {
        // Act
        var result = _transactionService.Deposit(1, 0m, "Zero deposit", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    [Category("Performance")]
    [CancelAfter(2000)]
    public void Deposit_PerformanceTest_CompleteWithin2Seconds()
    {
        // Act - Should complete within 2 seconds
        var result = _transactionService.Deposit(1, 1000m, "Performance test", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    [Category("Critical")]
    [Retry(3)]
    public void Deposit_RetryableOperation_SucceedsEventually()
    {
        // This test retries up to 3 times if it fails
        // Useful for timing-sensitive operations

        // Act
        var result = _transactionService.Deposit(1, 500m, "Retry test", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_CallsAuditLogWithCorrectEventType()
    {
        // Arrange
        _mockAudit.Setup(a => a.Log(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>()));

        // Act
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");

        // Assert
        _mockAudit.Verify(a => a.Log(
            It.Is<string>(e => e == "DEPOSIT"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_RepositoryAddIsCalledOnce()
    {
        // Arrange
        _mockTxnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));

        // Act
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");

        // Assert
        _mockTxnRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_GeneratesTransactionReference()
    {
        // Arrange
        Transaction? capturedTxn = null;
        _mockTxnRepo.Setup(r => r.Add(It.IsAny<Transaction>()))
            .Callback<Transaction>(t => capturedTxn = t);

        // Act
        var result = _transactionService.Deposit(1, 500m, "Test", "TELLER01");

        // Assert
        Assert.That(result.Data!.TransactionReference, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Data.TransactionReference, Does.StartWith("TXN-"));
    }

    [Test]
    [Category("Critical")]
    public void Deposit_MultipleAssertions_AllMustPass()
    {
        // Arrange
        const decimal amount = 1500m;
        decimal originalBalance = _testAccount.Balance;

        // Act
        var result = _transactionService.Deposit(1, amount, "Multi-assert test", "TELLER01");

        // Assert - Multiple assertions checked together
        Assert.Multiple((global::System.Action)(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Amount, Is.EqualTo(amount));
            Assert.That(result.Data.Type, Is.EqualTo(TransactionType.Deposit));
            Assert.That(result.Data.Status, Is.EqualTo(TransactionStatus.Completed));
            Assert.That(result.Message, Does.Contain("successful"));
        }));
    }

    [Test]
    [Category("Critical")]
    public void Deposit_MaximumTransactionLimit_ExceedsLimit()
    {
        // Act
        var result = _transactionService.Deposit(1, 60000m, "Over limit", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("exceed"));
    }

    [Test]
    [Category("Critical")]
    [TestCaseSource(nameof(GetDepositTestCases))]
    public void Deposit_WithComplexTestData_VariousScenarios(DepositTestCase testCase)
    {
        // Arrange
        if (!testCase.IsAccountActive)
        {
            _testAccount.Status = AccountStatus.Closed;
            _mockAccountRepo.Setup(r => r.GetById(1)).Returns(_testAccount);
        }

        // Act
        var result = _transactionService.Deposit(1, testCase.Amount, testCase.Description, "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.EqualTo(testCase.ShouldSucceed));
        if (!testCase.ShouldSucceed)
        {
            Assert.That(result.Message, Does.Contain(testCase.ExpectedErrorFragment));
        }
    }

    // TestCaseSource data provider
    private static IEnumerable<DepositTestCase> GetDepositTestCases()
    {
        yield return new DepositTestCase
        {
            Amount = 500m,
            Description = "Normal deposit",
            IsAccountActive = true,
            ShouldSucceed = true,
            ExpectedErrorFragment = ""
        };

        yield return new DepositTestCase
        {
            Amount = 55000m,
            Description = "Exceeds limit",
            IsAccountActive = true,
            ShouldSucceed = false,
            ExpectedErrorFragment = "exceed"
        };

        yield return new DepositTestCase
        {
            Amount = 500m,
            Description = "Closed account",
            IsAccountActive = false,
            ShouldSucceed = false,
            ExpectedErrorFragment = "Active"
        };
    }

    [Test]
    [Category("Critical")]
    public void Deposit_LastActivityDateUpdated_ToCurrentTime()
    {
        // Arrange
        var beforeDeposit = DateTime.UtcNow;

        // Act
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");

        // Assert
        _mockAccountRepo.Verify(r => r.Update(It.Is<Account>(a => 
            a.LastActivityDate >= beforeDeposit && 
            a.LastActivityDate <= DateTime.UtcNow)), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_CombinedWithValues_MultipleAmounts([Values(100, 250, 500, 1000, 5000)] decimal amount)
    {
        // This test runs multiple times with different values
        // Generates combinatorial test coverage

        // Act
        var result = _transactionService.Deposit(1, amount, $"Value test {amount}", "TELLER01");

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!.Amount, Is.EqualTo(amount).Within(0.01m));
    }

    /// <summary>
    /// Test data class for complex deposit scenarios
    /// </summary>
    public class DepositTestCase
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsAccountActive { get; set; }
        public bool ShouldSucceed { get; set; }
        public string ExpectedErrorFragment { get; set; } = string.Empty;
    }
}
