using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>TC-REP-001..011</summary>
[TestFixture]
public class ReportingTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<ITransactionRepository> _txnRepo = null!;
    private Mock<IAuditRepository> _auditRepo = null!;
    private ReportingService _svc = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _txnRepo = new Mock<ITransactionRepository>();
        _auditRepo = new Mock<IAuditRepository>();
        // ReportingService constructor from earlier read
        _svc = CreateReportingService();
    }

    private ReportingService CreateReportingService()
    {
        // Inspect constructor needs
        return new ReportingService(_accountRepo.Object, _txnRepo.Object, _auditRepo.Object);
    }

    private Account SeedAccount(int id = 1)
    {
        var a = new Account
        {
            Id = id,
            AccountNumber = "BC1000000001",
            Status = AccountStatus.Active,
            Balance = 500m,
            DateOpened = DateTime.UtcNow.AddMonths(-3)
        };
        _accountRepo.Setup(r => r.GetById(id)).Returns(a);
        return a;
    }

    /// <summary>TC-REP-001</summary>
    [Test]
    public void GenerateStatement_WithTransactions_Succeeds()
    {
        SeedAccount();
        var from = DateTime.UtcNow.AddMonths(-1);
        var to = DateTime.UtcNow;
        _txnRepo.Setup(r => r.GetByDateRange(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<Transaction>
        {
            new() { Amount = 100m, Type = TransactionType.Deposit, Timestamp = DateTime.UtcNow.AddDays(-5) }
        });

        var result = _svc.GenerateStatement(1, from, to);
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(result.Data, Is.Not.Null);
    }

    /// <summary>TC-REP-002</summary>
    [Test]
    public void GenerateStatement_ZeroTransactions_Succeeds()
    {
        SeedAccount();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        _txnRepo.Setup(r => r.GetByDateRange(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<Transaction>());
        var result = _svc.GenerateStatement(1, from, to);
        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    /// <summary>TC-REP-006</summary>
    [Test]
    [Category("Negative")]
    public void GenerateStatement_NonExistentAccount_Fails()
    {
        _accountRepo.Setup(r => r.GetById(99)).Returns((Account?)null);
        var result = _svc.GenerateStatement(99, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        Assert.That(result.IsSuccess, Is.False);
    }

    /// <summary>TC-REP-007</summary>
    [Test]
    [Category("Negative")]
    public void GenerateStatement_EndBeforeStart_Fails()
    {
        SeedAccount();
        var result = _svc.GenerateStatement(1, DateTime.UtcNow, DateTime.UtcNow.AddDays(-5));
        Assert.That(result.IsSuccess, Is.False);
    }

    /// <summary>TC-REP-009</summary>
    [Test]
    [Category("Negative")]
    public void GenerateStatement_InvertedHandledAsInvalid_Fails()
    {
        SeedAccount();
        var result = _svc.GenerateStatement(1, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(-1));
        Assert.That(result.IsSuccess, Is.False);
    }

    /// <summary>TC-REP-010</summary>
    [Test]
    public void GenerateStatement_SingleDay_Succeeds()
    {
        SeedAccount();
        var day = DateTime.UtcNow.Date;
        _txnRepo.Setup(r => r.GetByDateRange(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<Transaction>());
        var result = _svc.GenerateStatement(1, day, day);
        Assert.That(result.IsSuccess, Is.True, result.Message);
    }

    /// <summary>TC-REP-004 / 005</summary>
    [Test]
    public void GenerateSummaryReport_Succeeds()
    {
        _accountRepo.Setup(r => r.GetAll()).Returns(new List<Account>
        {
            new() { Type = AccountType.Savings, Balance = 100 },
            new() { Type = AccountType.Current, Balance = 200 }
        });
        var result = _svc.GenerateSummaryReport(DateTime.UtcNow);
        Assert.That(result.IsSuccess, Is.True, result.Message);
        Assert.That(result.Data, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>TC-REP-008</summary>
    [Test]
    public void GetAuditLog_ReturnsEntries()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        _auditRepo.Setup(r => r.GetByDateRange(from, to)).Returns(new List<AuditLog>
        {
            new() { EventType = "DEPOSIT", Username = "t1" }
        });
        var result = _svc.GetAuditLog(from, to);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data!, Has.Count.EqualTo(1));
    }

    /// <summary>TC-REP-011</summary>
    [Test]
    public void GenerateStatement_AccountOpenedToday_NoHistory_Succeeds()
    {
        var a = SeedAccount();
        a.DateOpened = DateTime.UtcNow.Date;
        var day = DateTime.UtcNow.Date;
        _txnRepo.Setup(r => r.GetByDateRange(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(new List<Transaction>());
        var result = _svc.GenerateStatement(1, day, day);
        Assert.That(result.IsSuccess, Is.True, result.Message);
    }
}
