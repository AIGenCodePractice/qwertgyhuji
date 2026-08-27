using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>
/// TC-TXN-014, 025–028 — Withdrawal decision table:
/// Rules: (1) sufficient funds (2) account Active (3) under daily limit.
/// </summary>
[TestFixture]
public class DecisionTableTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<ITransactionRepository> _txnRepo = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _svc = null!;

    [SetUp]
    public void SetUp()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _txnRepo = new Mock<ITransactionRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txnRepo.Setup(r => r.ReferenceExists(It.IsAny<string>())).Returns(false);
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    private void Seed(AccountStatus status, decimal balance, decimal dailyUsed, decimal dailyLimit)
    {
        var a = new Account
        {
            Id = 1,
            Status = status,
            Balance = balance,
            DailyWithdrawnToday = dailyUsed,
            DailyWithdrawalLimit = dailyLimit
        };
        _accountRepo.Setup(r => r.GetById(1)).Returns(a);
    }

    /// <summary>TC-TXN-025 — all rules true → approve</summary>
    [Test]
    [Category("Critical")]
    [TestCase(true, true, true, true)]
    public void DecisionTable_Approve_WhenAllRulesTrue(bool funds, bool active, bool underLimit, bool expectedSuccess)
    {
        Seed(AccountStatus.Active, 1000m, 0m, 5000m);
        var result = _svc.Withdraw(1, 100m, "dt", "t");
        Assert.That(result.IsSuccess, Is.EqualTo(expectedSuccess), result.Message);
    }

    /// <summary>TC-TXN-026 — insufficient funds only</summary>
    [Test]
    public void DecisionTable_Reject_InsufficientFunds()
    {
        Seed(AccountStatus.Active, 50m, 0m, 5000m);
        var result = _svc.Withdraw(1, 100m, "dt", "t");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("Insufficient").IgnoreCase);
    }

    /// <summary>TC-TXN-027 — not Active only</summary>
    [Test]
    public void DecisionTable_Reject_AccountNotActive()
    {
        Seed(AccountStatus.Dormant, 1000m, 0m, 5000m);
        var result = _svc.Withdraw(1, 100m, "dt", "t");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("Active").IgnoreCase);
    }

    /// <summary>TC-TXN-028 — daily limit only</summary>
    [Test]
    public void DecisionTable_Reject_DailyLimitExceeded()
    {
        Seed(AccountStatus.Active, 1000m, 5000m, 5000m);
        var result = _svc.Withdraw(1, 1m, "dt", "t");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Message, Does.Contain("limit").IgnoreCase);
    }

    /// <summary>TC-TXN-014 — all rules false</summary>
    [Test]
    public void DecisionTable_Reject_AllRulesFail()
    {
        Seed(AccountStatus.Closed, 0m, 5000m, 100m);
        var result = _svc.Withdraw(1, 50m, "dt", "t");
        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    [TestCaseSource(nameof(DecisionColumns))]
    public void DecisionTable_AllColumns(AccountStatus status, decimal balance, decimal used, decimal limit, decimal amount, bool expectOk)
    {
        Seed(status, balance, used, limit);
        var result = _svc.Withdraw(1, amount, "src", "t");
        Assert.That(result.IsSuccess, Is.EqualTo(expectOk), result.Message);
    }

    public static IEnumerable<TestCaseData> DecisionColumns()
    {
        // Full decision table for (sufficient funds, Active status, under daily limit) = 2^3 = 8 columns
        // amount always 100m; balance/used/limit encode the three boolean rules
        yield return new TestCaseData(AccountStatus.Active, 500m, 0m, 1000m, 100m, true)
            .SetName("R1_Funds_Active_UnderLimit_APPROVE");
        yield return new TestCaseData(AccountStatus.Active, 500m, 1000m, 1000m, 100m, false)
            .SetName("R2_Funds_Active_OverLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Closed, 500m, 0m, 1000m, 100m, false)
            .SetName("R3_Funds_NotActive_UnderLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Closed, 500m, 1000m, 1000m, 100m, false)
            .SetName("R4_Funds_NotActive_OverLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Active, 50m, 0m, 1000m, 100m, false)
            .SetName("R5_NoFunds_Active_UnderLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Active, 50m, 1000m, 1000m, 100m, false)
            .SetName("R6_NoFunds_Active_OverLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Dormant, 50m, 0m, 1000m, 100m, false)
            .SetName("R7_NoFunds_NotActive_UnderLimit_REJECT");
        yield return new TestCaseData(AccountStatus.Closed, 0m, 5000m, 100m, 100m, false)
            .SetName("R8_NoFunds_NotActive_OverLimit_REJECT");
    }
}

