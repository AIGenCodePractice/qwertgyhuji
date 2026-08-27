using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

[TestFixture]
public class WithdrawalTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<ITransactionRepository> _txnRepo = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _svc = null!;
    private Account _account = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => TestContext.Out.WriteLine("WithdrawalTests OneTimeSetUp");

    [OneTimeTearDown]
    public void OneTimeTearDown() => TestContext.Out.WriteLine("WithdrawalTests OneTimeTearDown");

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _txnRepo = new Mock<ITransactionRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _validator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

        _account = new Account
        {
            Id = 1,
            AccountNumber = "BC1000000001",
            Status = AccountStatus.Active,
            Balance = 1000m,
            DailyWithdrawalLimit = 5000m,
            DailyWithdrawnToday = 0m
        };
        _accountRepo.Setup(r => r.GetById(1)).Returns(_account);
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txnRepo.Setup(r => r.ReferenceExists(It.IsAny<string>())).Returns(false);
        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    [Test]
    [Category("Critical")]
    [TestCase(1000.00, 200.00, 800.00)]
    [TestCase(500.00, 100.00, 400.00)]
    [TestCase(50.00, 10.00, 40.00)]
    public void Withdraw_PartialAmount_UpdatesBalance(decimal initial, decimal amount, decimal expected)
    {
        _account.Balance = initial;
        var result = _svc.Withdraw(1, amount, "ATM", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(_account.Balance, Is.EqualTo(expected));
            Assert.That(result.Data!.Type, Is.EqualTo(TransactionType.Withdrawal));
        }
    }

    [Test]
    [Category("Boundary")]
    public void Withdraw_ExactBalance_SucceedsAndZeroBalance()
    {
        _account.Balance = 250.50m;
        var result = _svc.Withdraw(1, 250.50m, "Full", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(_account.Balance, Is.Zero);
        }
    }

    [Test]
    [Category("Negative")]
    [TestCase(100.00, 100.01)]
    [TestCase(50.00, 51.00)]
    public void Withdraw_ExceedsBalance_ReturnsFailure(decimal balance, decimal amount)
    {
        _account.Balance = balance;
        var result = _svc.Withdraw(1, amount, "Over", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("Insufficient").IgnoreCase);
            _txnRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
        }
    }

    [Test]
    [Category("Negative")]
    public void Withdraw_ClosedAccount_ReturnsFailure()
    {
        _account.Status = AccountStatus.Closed;
        var result = _svc.Withdraw(1, 10m, "x", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("Active").IgnoreCase);
        }
    }

    [Test]
    [Category("Negative")]
    public void Withdraw_DailyLimitReached_ReturnsFailure()
    {
        _account.Balance = 10000m;
        _account.DailyWithdrawalLimit = 500m;
        _account.DailyWithdrawnToday = 500m;
        var result = _svc.Withdraw(1, 0.01m, "over limit", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("limit").IgnoreCase);
        }
    }

    [Test]
    [Category("Negative")]
    [TestCase(0)]
    [TestCase(-1)]
    public void Withdraw_NonPositive_ReturnsFailure(decimal amount)
    {
        var result = _svc.Withdraw(1, amount, "x", "teller1");
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    [CancelAfter(2000)]
    [Category("Performance")]
    public void Withdraw_CompletesWithinTimeout()
    {
        var result = _svc.Withdraw(1, 10m, "perf", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(_account.Balance, Is.InRange(0m, 1000m));
        }
    }

    [Test]
    [Category("Critical")]
    public void Withdraw_MultipleAsserts_ValidatesCompletedWithdrawal()
    {
        _account.Balance = 500m;
        var result = _svc.Withdraw(1, 50m, "multi", "teller1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Amount, Is.EqualTo(50m));
            Assert.That(_account.Balance, Is.EqualTo(450m));
        }
    }

    [Test]
    [Category("Negative")]
    public void Withdraw_PersistenceFailure_ThrowsConfiguredRepositoryException()
    {
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()))
            .Throws(new InvalidOperationException("Simulated transaction repository failure."));

        Assert.That((TestDelegate)(() => _svc.Withdraw(1, 50m, "Persistence failure", "teller1")),
            Throws.TypeOf<InvalidOperationException>());
        _txnRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
    }
}
