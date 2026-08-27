using NUnit.Framework;
using System.Diagnostics;
using Moq;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;

namespace BankCore.Tests.NUnit;

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
    public void OneTimeSetup() => TestContext.Out.WriteLine("OneTimeSetUp: Initializing test fixtures");

    [OneTimeTearDown]
    public void OneTimeTearDown() => TestContext.Out.WriteLine("OneTimeTearDown: Cleaning up shared resources");

    [SetUp]
    public void Setup()
    {
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockTxnRepo = new Mock<ITransactionRepository>();
        _mockValidator = new Mock<IValidationService>();
        _mockAudit = new Mock<IAuditService>();
        _mockValidator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

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
        _transactionService = new TransactionService(_mockAccountRepo.Object, _mockTxnRepo.Object, _mockValidator.Object, _mockAudit.Object);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_WithValidAmount_ReturnsSuccess()
    {
        var result = _transactionService.Deposit(1, 1000m, "Regular deposit", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Amount, Is.EqualTo(1000m));
        }
    }

    [Test]
    [Category("Critical")]
    [TestCase(100, "R100 deposit")]
    [TestCase(500, "R500 deposit")]
    [TestCase(1000, "R1000 deposit")]
    [TestCase(5000, "R5000 deposit")]
    public void Deposit_WithVariousAmounts_UpdatesAccountBalance(decimal amount, string description)
    {
        var balanceBefore = _testAccount.Balance;
        var result = _transactionService.Deposit(1, amount, description, "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Amount, Is.EqualTo(amount));
            _mockAccountRepo.Verify(r => r.Update(It.Is<Account>(a => a.Balance == balanceBefore + amount)), Times.Once);
        }
    }

    [Test]
    [Category("Critical")]
    public void Deposit_ToInactiveAccount_ReturnsFailed()
    {
        _testAccount.Status = AccountStatus.Dormant;
        var result = _transactionService.Deposit(1, 500m, "Test", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("Active"));
        }
    }

    [Test]
    [Category("Critical")]
    public void Deposit_NegativeAmount_ReturnsFailed()
    {
        var result = _transactionService.Deposit(1, -100m, "Negative deposit", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("greater than zero"));
        }
    }

    [Test]
    [Category("Critical")]
    public void Deposit_ZeroAmount_ReturnsFailed()
    {
        var result = _transactionService.Deposit(1, 0m, "Zero deposit", "TELLER01");
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    [Category("Performance")]
    [CancelAfter(2000)]
    public void Deposit_PerformanceTest_1000SequentialOperationsCompleteWithin2Seconds()
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 1000; i++)
        {
            var result = _transactionService.Deposit(1, 1m, "Performance test", "TELLER01");
            Assert.That(result.IsSuccess, Is.True, result.Message);
        }
        stopwatch.Stop();
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
    }

    [Test]
    [Category("Performance")]
    [Retry(3)]
    [CancelAfter(2000)]
    public void Deposit_PerformanceMeasurement_TimingSensitiveOperationMeetsThreshold()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = _transactionService.Deposit(1, 500m, "Timing-sensitive retry test", "TELLER01");
        stopwatch.Stop();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
        }
    }

    [Test]
    [Category("Negative")]
    public void Deposit_PersistenceFailure_PropagatesConfiguredRepositoryException()
    {
        _mockTxnRepo.Setup(r => r.Add(It.IsAny<Transaction>()))
            .Throws(new InvalidOperationException("Simulated transaction repository failure."));

        Assert.That((TestDelegate)(() => _transactionService.Deposit(1, 500m, "Persistence failure", "TELLER01")),
            Throws.TypeOf<InvalidOperationException>());
        _mockTxnRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_CallsAuditLogWithCorrectEventType()
    {
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");
        _mockAudit.Verify(a => a.Log("DEPOSIT", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_RepositoryAddIsCalledOnce()
    {
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");
        _mockTxnRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_GeneratesTransactionReference()
    {
        var result = _transactionService.Deposit(1, 500m, "Test", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Data!.TransactionReference, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Data.TransactionReference, Does.StartWith("TXN-"));
        }
    }

    [Test]
    [Category("Critical")]
    public void Deposit_MultipleAssertions_AllMustPass()
    {
        const decimal amount = 1500m;
        var result = _transactionService.Deposit(1, amount, "Multi-assert test", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Amount, Is.EqualTo(amount));
            Assert.That(result.Data.Type, Is.EqualTo(TransactionType.Deposit));
            Assert.That(result.Data.Status, Is.EqualTo(TransactionStatus.Completed));
            Assert.That(result.Message, Does.Contain("successful"));
        }
    }

    [Test]
    [Category("Critical")]
    public void Deposit_MaximumTransactionLimit_ExceedsLimit()
    {
        var result = _transactionService.Deposit(1, 60000m, "Over limit", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("exceed"));
        }
    }

    [Test]
    [Category("Critical")]
    [TestCaseSource(nameof(GetDepositTestCases))]
    public void Deposit_WithComplexTestData_VariousScenarios(DepositTestCase testCase)
    {
        if (!testCase.IsAccountActive)
            _testAccount.Status = AccountStatus.Closed;

        var result = _transactionService.Deposit(1, testCase.Amount, testCase.Description, "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.EqualTo(testCase.ShouldSucceed));
            if (!testCase.ShouldSucceed)
                Assert.That(result.Message, Does.Contain(testCase.ExpectedErrorFragment));
        }
    }

    private static IEnumerable<DepositTestCase> GetDepositTestCases()
    {
        yield return new DepositTestCase { Amount = 500m, Description = "Normal deposit", IsAccountActive = true, ShouldSucceed = true };
        yield return new DepositTestCase { Amount = 55000m, Description = "Exceeds limit", IsAccountActive = true, ShouldSucceed = false, ExpectedErrorFragment = "exceed" };
        yield return new DepositTestCase { Amount = 500m, Description = "Closed account", IsAccountActive = false, ShouldSucceed = false, ExpectedErrorFragment = "Active" };
    }

    [Test]
    [Category("Critical")]
    public void Deposit_LastActivityDateUpdated_ToCurrentTime()
    {
        var beforeDeposit = DateTime.UtcNow;
        _transactionService.Deposit(1, 500m, "Test", "TELLER01");
        _mockAccountRepo.Verify(r => r.Update(It.Is<Account>(a => a.LastActivityDate >= beforeDeposit && a.LastActivityDate <= DateTime.UtcNow)), Times.Once);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_CombinedWithValues_MultipleAmounts([Values(100, 250, 500, 1000, 5000)] decimal amount)
    {
        var result = _transactionService.Deposit(1, amount, $"Value test {amount}", "TELLER01");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data!.Amount, Is.EqualTo(amount).Within(0.01m));
        }
    }

    public class DepositTestCase
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsAccountActive { get; set; }
        public bool ShouldSucceed { get; set; }
        public string ExpectedErrorFragment { get; set; } = string.Empty;
    }
}
