using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>
/// TC-TXN-003, 005, 006, 008, 012, 017, 022, 023, 024
/// </summary>
[TestFixture]
public class TransferTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<ITransactionRepository> _txnRepo = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _svc = null!;
    private Account _from = null!;
    private Account _to = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _txnRepo = new Mock<ITransactionRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _validator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

        _from = new Account { Id = 1, AccountNumber = "BC1000000001", Status = AccountStatus.Active, Balance = 1000m, DailyWithdrawalLimit = 50000m, DailyWithdrawnToday = 0m };
        _to = new Account { Id = 2, AccountNumber = "BC1000000002", Status = AccountStatus.Active, Balance = 100m, DailyWithdrawalLimit = 50000m, DailyWithdrawnToday = 0m };

        _accountRepo.Setup(r => r.GetById(1)).Returns(_from);
        _accountRepo.Setup(r => r.GetById(2)).Returns(_to);
        _accountRepo.Setup(r => r.GetById(It.Is<int>(id => id != 1 && id != 2))).Returns((Account?)null);
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txnRepo.Setup(r => r.ReferenceExists(It.IsAny<string>())).Returns(false);

        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    /// <summary>TC-TXN-003</summary>
    [Test]
    [Category("Critical")]
    public void Transfer_BetweenActiveAccounts_MovesFunds()
    {
        var result = _svc.Transfer(1, 2, 200m, "pay", "teller1");
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(_from.Balance, Is.EqualTo(800m));
        Assert.That(_to.Balance, Is.EqualTo(300m));
    }

    /// <summary>TC-TXN-005</summary>
    [Test]
    [Category("Boundary")]
    public void Transfer_ExactRemainingBalance_Succeeds()
    {
        var result = _svc.Transfer(1, 2, 1000m, "all", "teller1");
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(_from.Balance, Is.EqualTo(0m));
        Assert.That(_to.Balance, Is.EqualTo(1100m));
    }

    /// <summary>TC-TXN-006</summary>
    [Test]
    public void Transfer_Sequential_ProducesCorrectFinalBalance()
    {
        _svc.Transfer(1, 2, 100m, "1", "t");
        _svc.Transfer(1, 2, 100m, "2", "t");
        _svc.Transfer(1, 2, 100m, "3", "t");
        Assert.That(_from.Balance, Is.EqualTo(700m));
        Assert.That(_to.Balance, Is.EqualTo(400m));
    }

    /// <summary>TC-TXN-008</summary>
    [Test]
    public void Transfer_ToNewlyCreatedAccount_Succeeds()
    {
        var newborn = new Account { Id = 3, AccountNumber = "BC1000000003", Status = AccountStatus.Active, Balance = 0m, DailyWithdrawalLimit = 50000m };
        _accountRepo.Setup(r => r.GetById(3)).Returns(newborn);
        var result = _svc.Transfer(1, 3, 50m, "welcome", "teller1");
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(newborn.Balance, Is.EqualTo(50m));
    }

    /// <summary>TC-TXN-012</summary>
    [Test]
    [Category("Negative")]
    public void Transfer_FromZeroBalance_ReturnsFailure()
    {
        _from.Balance = 0m;
        var result = _svc.Transfer(1, 2, 10m, "x", "t");
        Assert.That(result.IsSuccess, Is.False);
    }

    /// <summary>TC-TXN-017</summary>
    [Test]
    [Category("Negative")]
    public void Transfer_ToNonExistent_ReturnsFailure()
    {
        var result = _svc.Transfer(1, 999, 10m, "x", "t");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("not found").IgnoreCase);
    }

    /// <summary>TC-TXN-022</summary>
    [Test]
    [Category("Boundary")]
    public void Transfer_MinimumAmount_Succeeds()
    {
        var result = _svc.Transfer(1, 2, 0.01m, "min", "t");
        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    /// <summary>TC-TXN-023</summary>
    [Test]
    [Category("Boundary")]
    public void Transfer_MaximumAllowed_Succeeds()
    {
        _from.Balance = 60000m;
        var result = _svc.Transfer(1, 2, 50000m, "max", "t");
        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    /// <summary>TC-TXN-024</summary>
    [Test]
    [Category("Negative")]
    public void Transfer_JustAboveMaximum_ReturnsFailure()
    {
        _from.Balance = 100000m;
        var result = _svc.Transfer(1, 2, 50000.01m, "over", "t");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("exceed").IgnoreCase);
    }

    [Test]
    [Category("Critical")]
    [TestCase(100.0, 200.0, 300.0)]
    [TestCase(50.0, 25.0, 10.0)]
    [TestCase(1.0, 1.0, 1.0)]
    public void Transfer_MultipleAmounts_AssertMultiple(decimal a, decimal b, decimal c)
    {
        // Three sequential transfers; Assert.Multiple checks all balances without failing fast
        _svc.Transfer(1, 2, a, "1", "t");
        _svc.Transfer(1, 2, b, "2", "t");
        var result = _svc.Transfer(1, 2, c, "3", "t");

        Assert.Multiple((global::System.Action)(() =>
        {
            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(_from.Balance, Is.EqualTo(1000m - a - b - c));
            Assert.That(_to.Balance, Is.EqualTo(100m + a + b + c));
            Assert.That(_from.Balance, Is.GreaterThanOrEqualTo(0m));
        }));
    }
}
