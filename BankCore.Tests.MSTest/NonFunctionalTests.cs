using System.Diagnostics;
using System.Text;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// REQ-NF-001 … REQ-NF-003 / TC-NF-* (unit-level proxies for non-functional requirements).
/// These tests guard application-level regressions; they are not presented as full system load tests.
/// </summary>
[TestClass]
public class NonFunctionalTests
{
    private Mock<IAccountRepository> _accounts = null!;
    private Mock<ITransactionRepository> _txns = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private TransactionService _txnService = null!;
    private Account _account = null!;

    [TestInitialize]
    public void Setup()
    {
        _accounts = new Mock<IAccountRepository>();
        _txns = new Mock<ITransactionRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _validator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

        _account = new Account
        {
            Id = 1,
            AccountNumber = "BC1000000001",
            Status = AccountStatus.Active,
            Balance = 1_000_000m,
            DailyWithdrawalLimit = 500_000m,
            DailyWithdrawnToday = 0m
        };
        _accounts.Setup(r => r.GetById(1)).Returns(_account);
        _accounts.Setup(r => r.Update(It.IsAny<Account>()));
        _txns.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txns.Setup(r => r.ReferenceExists(It.IsAny<string>())).Returns(false);

        _txnService = new TransactionService(_accounts.Object, _txns.Object, _validator.Object, _audit.Object);
    }

    /// <summary>TC-NF-PERF-001 — 1,000 sequential application-level deposits complete under the time budget.</summary>
    [TestMethod]
    [TestCategory("Performance")]
    public void TC_NF_PERF_001_SequentialDeposits_CompleteWithinBudget()
    {
        const int count = 1_000;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var result = _txnService.Deposit(1, 1m, $"perf-{i}", "TELLER01");
            Assert.IsTrue(result.IsSuccess, result.Message);
        }
        sw.Stop();

        _txns.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Exactly(count));
        Assert.AreEqual(1_001_000m, _account.Balance);
        Assert.IsTrue(sw.ElapsedMilliseconds < 15_000,
            $"{count} deposits took {sw.ElapsedMilliseconds}ms (budget 15s)");
    }

    /// <summary>TC-NF-PERF-002 — repeated transaction history requests remain responsive.</summary>
    [TestMethod]
    [TestCategory("Performance")]
    public void TC_NF_PERF_002_RapidHistoryReads_NoExcessiveTime()
    {
        _txns.Setup(r => r.GetByAccountId(1)).Returns(new List<Transaction>());

        const int count = 500;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var result = _txnService.GetTransactionHistory(1);
            Assert.IsTrue(result.IsSuccess, result.Message);
        }
        sw.Stop();

        _txns.Verify(r => r.GetByAccountId(1), Times.Exactly(count));
        Assert.IsTrue(sw.ElapsedMilliseconds < 5_000,
            $"{count} transaction history requests took {sw.ElapsedMilliseconds}ms (budget 5s)");
    }

    /// <summary>TC-NF-ROB-001 — oversized description is handled without an unhandled exception.</summary>
    [TestMethod]
    [TestCategory("Robustness")]
    public void TC_NF_ROB_001_OversizedDescription_IsHandledGracefully()
    {
        var huge = new string('X', 50_000);
        var result = _txnService.Deposit(1, 10m, huge, "TELLER01");

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message));
    }

    /// <summary>TC-NF-ROB-002 — rapid successive mixed operations preserve a deterministic balance.</summary>
    [TestMethod]
    [TestCategory("Robustness")]
    public void TC_NF_ROB_002_RapidMixedOperations_PreserveExpectedBalance()
    {
        const int count = 50;
        for (int i = 0; i < count; i++)
        {
            var deposit = _txnService.Deposit(1, 5m, "d", "T");
            var withdrawal = _txnService.Withdraw(1, 1m, "w", "T");
            Assert.IsTrue(deposit.IsSuccess, deposit.Message);
            Assert.IsTrue(withdrawal.IsSuccess, withdrawal.Message);
        }

        Assert.AreEqual(1_000_000m + (count * 4m), _account.Balance);
        Assert.AreEqual(count, _account.DailyWithdrawnToday);
    }

    [TestMethod]
    [TestCategory("Security")]
    public void TC_NF_SEC_001_BlankSession_CannotAuthenticate()
    {
        var mockUsers = new Mock<IUserRepository>();
        var mockSessions = new Mock<ISessionRepository>();
        var mockHasher = new Mock<IPasswordHasher>();
        var mockAudit = new Mock<IAuditService>();
        var mockVal = new Mock<IValidationService>();
        mockSessions.Setup(s => s.GetByToken(It.IsAny<string>())).Returns((Session?)null);

        var auth = new AuthService(mockUsers.Object, mockSessions.Object, mockHasher.Object, mockAudit.Object, mockVal.Object);

        Assert.IsFalse(auth.ValidateSession("").IsSuccess);
        Assert.IsFalse(auth.ValidateSession("forged-token").IsSuccess);
    }

    [TestMethod]
    [TestCategory("Security")]
    public void TC_NF_SEC_002_UserModel_UsesHashFields_NotPlainPasswordProperty()
    {
        var userType = typeof(User);
        Assert.IsNull(userType.GetProperty("Password"));
        Assert.IsNotNull(userType.GetProperty("PasswordHash"));
        Assert.IsNotNull(userType.GetProperty("Salt"));
    }
}
