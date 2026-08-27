using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>TC-TXN-009, TC-TXN-010</summary>
[TestFixture]
public class ReversalTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<ITransactionRepository> _txnRepo = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _svc = null!;
    private Account _account = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _txnRepo = new Mock<ITransactionRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _account = new Account { Id = 1, Status = AccountStatus.Active, Balance = 1500m };
        _accountRepo.Setup(r => r.GetById(1)).Returns(_account);
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txnRepo.Setup(r => r.Update(It.IsAny<Transaction>()));
        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    /// <summary>TC-TXN-009</summary>
    [Test]
    [Category("Critical")]
    public void Reverse_SuccessfulDeposit_WithinWindow_Succeeds()
    {
        var original = new Transaction
        {
            Id = 10,
            TransactionReference = "TXN-DEP-001",
            AccountId = 1,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Amount = 500m,
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };
        _txnRepo.Setup(r => r.GetByReference("TXN-DEP-001")).Returns(original);

        var result = _svc.ReverseTransaction("TXN-DEP-001", "customer request", "teller1");

        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(original.Status, Is.EqualTo(TransactionStatus.Reversed));
        Assert.That(_account.Balance, Is.EqualTo(1000m));
    }

    /// <summary>TC-TXN-010 — partial reversal is not supported; full amount only</summary>
    [Test]
    [Category("Negative")]
    public void Reverse_AlreadyReversed_ReturnsFailure()
    {
        var original = new Transaction
        {
            TransactionReference = "TXN-DEP-002",
            AccountId = 1,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Reversed,
            Amount = 5000m,
            Timestamp = DateTime.UtcNow
        };
        _txnRepo.Setup(r => r.GetByReference("TXN-DEP-002")).Returns(original);

        var result = _svc.ReverseTransaction("TXN-DEP-002", "partial?", "teller1");

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("already").IgnoreCase);
    }

    [Test]
    [Category("Negative")]
    public void Reverse_OutsideWindow_ReturnsFailure()
    {
        var original = new Transaction
        {
            TransactionReference = "TXN-OLD",
            AccountId = 1,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            Amount = 100m,
            Timestamp = DateTime.UtcNow.AddHours(-25)
        };
        _txnRepo.Setup(r => r.GetByReference("TXN-OLD")).Returns(original);

        var result = _svc.ReverseTransaction("TXN-OLD", "late", "teller1");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("window").IgnoreCase);
    }
}
