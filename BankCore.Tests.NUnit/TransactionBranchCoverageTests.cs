using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;
using NUnit.Framework;

namespace BankCore.Tests.NUnit;

[TestFixture]
public class TransactionBranchCoverageTests
{
    private Mock<IAccountRepository> _accounts = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private Mock<IValidationService> _validation = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _svc = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IAccountRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _validation = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _svc = new TransactionService(_accounts.Object, _transactions.Object, _validation.Object, _audit.Object);
    }

    private static Account Account(int id, decimal balance = 1000m, AccountStatus status = AccountStatus.Active) => new()
    {
        Id = id, AccountNumber = $"BC{id:0000000000}", Balance = balance, Status = status,
        DailyWithdrawalLimit = 5000m, DailyWithdrawnToday = 0m
    };

    [TestCase(1, 1, 10)]
    [TestCase(1, 2, 0)]
    [TestCase(1, 2, 50001)]
    public void Transfer_EarlyValidationBranches_ReturnFailure(int from, int to, decimal amount)
    {
        var result = _svc.Transfer(from, to, amount, "test", "user");
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void Transfer_RepositoryAndStatusAndFundsBranches_ReturnFailure()
    {
        _accounts.Setup(r => r.GetById(1)).Returns((Account?)null);
        Assert.That(_svc.Transfer(1, 2, 10m, "x", "u").IsSuccess, Is.False);

        _accounts.Setup(r => r.GetById(1)).Returns(Account(1));
        _accounts.Setup(r => r.GetById(2)).Returns((Account?)null);
        Assert.That(_svc.Transfer(1, 2, 10m, "x", "u").IsSuccess, Is.False);

        _accounts.Setup(r => r.GetById(1)).Returns(Account(1, status: AccountStatus.Closed));
        _accounts.Setup(r => r.GetById(2)).Returns(Account(2));
        Assert.That(_svc.Transfer(1, 2, 10m, "x", "u").IsSuccess, Is.False);

        _accounts.Setup(r => r.GetById(1)).Returns(Account(1));
        _accounts.Setup(r => r.GetById(2)).Returns(Account(2, status: AccountStatus.Closed));
        Assert.That(_svc.Transfer(1, 2, 10m, "x", "u").IsSuccess, Is.False);

        _accounts.Setup(r => r.GetById(1)).Returns(Account(1, 5m));
        _accounts.Setup(r => r.GetById(2)).Returns(Account(2));
        Assert.That(_svc.Transfer(1, 2, 10m, "x", "u").IsSuccess, Is.False);
    }

    [Test]
    public void Transfer_DailyLimitAndSuccessBranches_AreCovered()
    {
        var from = Account(1);
        from.DailyWithdrawalLimit = 10m;
        from.DailyWithdrawnToday = 10m;
        _accounts.Setup(r => r.GetById(1)).Returns(from);
        _accounts.Setup(r => r.GetById(2)).Returns(Account(2));
        Assert.That(_svc.Transfer(1, 2, 1m, "x", "u").IsSuccess, Is.False);

        from.DailyWithdrawnToday = 0m;
        var to = Account(2, 100m);
        _accounts.Setup(r => r.GetById(2)).Returns(to);
        var result = _svc.Transfer(1, 2, 50m, "x", "u");
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(from.Balance, Is.EqualTo(950m));
        Assert.That(to.Balance, Is.EqualTo(150m));
    }

    [Test]
    public void ReverseTransaction_CoversValidationStatusTimeAndAccountBranches()
    {
        Assert.That(_svc.ReverseTransaction(" ", "x", "u").IsSuccess, Is.False);

        _transactions.Setup(r => r.GetByReference("missing")).Returns((Transaction?)null);
        Assert.That(_svc.ReverseTransaction("missing", "x", "u").IsSuccess, Is.False);

        var reversed = new Transaction { TransactionReference = "rev", Status = TransactionStatus.Reversed };
        _transactions.Setup(r => r.GetByReference("rev")).Returns(reversed);
        Assert.That(_svc.ReverseTransaction("rev", "x", "u").IsSuccess, Is.False);

        var pending = new Transaction { TransactionReference = "pending", Status = TransactionStatus.Pending };
        _transactions.Setup(r => r.GetByReference("pending")).Returns(pending);
        Assert.That(_svc.ReverseTransaction("pending", "x", "u").IsSuccess, Is.False);

        var old = new Transaction { TransactionReference = "old", Status = TransactionStatus.Completed, Timestamp = DateTime.UtcNow.AddHours(-25) };
        _transactions.Setup(r => r.GetByReference("old")).Returns(old);
        Assert.That(_svc.ReverseTransaction("old", "x", "u").IsSuccess, Is.False);

        var noAccount = new Transaction { TransactionReference = "na", AccountId = 1, Status = TransactionStatus.Completed, Timestamp = DateTime.UtcNow };
        _transactions.Setup(r => r.GetByReference("na")).Returns(noAccount);
        _accounts.Setup(r => r.GetById(1)).Returns((Account?)null);
        Assert.That(_svc.ReverseTransaction("na", "x", "u").IsSuccess, Is.False);
    }

    [Test]
    public void ReverseTransaction_CoversDepositInsufficientWithdrawalAndSuccessBranches()
    {
        var deposit = new Transaction { TransactionReference = "dep", AccountId = 1, Type = TransactionType.Deposit, Amount = 100m, Status = TransactionStatus.Completed, Timestamp = DateTime.UtcNow };
        _transactions.Setup(r => r.GetByReference("dep")).Returns(deposit);
        _accounts.Setup(r => r.GetById(1)).Returns(Account(1, 50m));
        Assert.That(_svc.ReverseTransaction("dep", "x", "u").IsSuccess, Is.False);

        var withdrawal = new Transaction { TransactionReference = "wd", AccountId = 1, Type = TransactionType.Withdrawal, Amount = 100m, Status = TransactionStatus.Completed, Timestamp = DateTime.UtcNow };
        var account = Account(1, 50m);
        _transactions.Setup(r => r.GetByReference("wd")).Returns(withdrawal);
        _accounts.Setup(r => r.GetById(1)).Returns(account);
        var result = _svc.ReverseTransaction("wd", "x", "u");
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(account.Balance, Is.EqualTo(150m));
        Assert.That(withdrawal.Status, Is.EqualTo(TransactionStatus.Reversed));
    }

    [Test]
    public void GetTransactionHistory_CoversMissingRangeAndAllBranches()
    {
        _accounts.Setup(r => r.GetById(1)).Returns((Account?)null);
        Assert.That(_svc.GetTransactionHistory(1).IsSuccess, Is.False);

        _accounts.Setup(r => r.GetById(1)).Returns(Account(1));
        _transactions.Setup(r => r.GetByAccountId(1)).Returns(new List<Transaction> { new() });
        Assert.That(_svc.GetTransactionHistory(1).Data, Has.Count.EqualTo(1));

        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        _transactions.Setup(r => r.GetByDateRange(1, from, to)).Returns(new List<Transaction> { new(), new() });
        Assert.That(_svc.GetTransactionHistory(1, from, to).Data, Has.Count.EqualTo(2));
    }
}
