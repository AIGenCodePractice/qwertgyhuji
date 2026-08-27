using System.Diagnostics;
using System.Text;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// REQ-NF-001 … REQ-NF-003 / TC-NF-* (unit-level proxies for non-functional requirements).
/// Full load tests would be integration/system; these guard obvious regressions.
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
        _validator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(true);

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

    /// <summary>TC-NF-PERF-001 — many sequential deposits complete under a soft time budget</summary>
    [TestMethod]
    [TestCategory("Performance")]
    public void TC_NF_PERF_001_SequentialDeposits_CompleteWithinBudget()
    {
        const int count = 200; // scaled unit proxy for "1000 sequential" (full load = system test)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var result = _txnService.Deposit(1, 1m, $"perf-{i}", "TELLER01");
            Assert.IsTrue(result.IsSuccess, result.Message);
        }
        sw.Stop();
        Assert.IsTrue(sw.ElapsedMilliseconds < 15_000,
            $"200 deposits took {sw.ElapsedMilliseconds}ms (budget 15s)");
    }

    /// <summary>TC-NF-PERF-002 — statement-sized loop stays responsive</summary>
    [TestMethod]
    [TestCategory("Performance")]
    public void TC_NF_PERF_002_RapidBalanceReads_NoExcessiveTime()
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 500; i++)
            _ = _accounts.Object.GetById(1);
        sw.Stop();
        Assert.IsTrue(sw.ElapsedMilliseconds < 5_000);
    }

    /// <summary>TC-NF-ROB-001 — oversized description does not crash the service</summary>
    [TestMethod]
    [TestCategory("Robustness")]
    public void TC_NF_ROB_001_OversizedDescription_DoesNotCrash()
    {
        var huge = new string('X', 50_000);
        var result = _txnService.Deposit(1, 10m, huge, "TELLER01");
        // Either succeeds or fails gracefully — must not throw
        Assert.IsNotNull(result);
    }

    /// <summary>TC-NF-ROB-002 — rapid successive mixed ops without exception</summary>
    [TestMethod]
    [TestCategory("Robustness")]
    public void TC_NF_ROB_002_RapidMixedOperations_NoCrash()
    {
        for (int i = 0; i < 50; i++)
        {
            Assert.IsNotNull(_txnService.Deposit(1, 5m, "d", "T"));
            Assert.IsNotNull(_txnService.Withdraw(1, 1m, "w", "T"));
        }
        Assert.IsTrue(_account.Balance > 0);
    }

    /// <summary>TC-NF-SEC-001 — blank/missing session cannot validate as authenticated</summary>
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

        var auth = new AuthService(mockUsers.Object, mockSessions.Object, mockHasher.Object,
            mockAudit.Object, mockVal.Object);

        Assert.IsFalse(auth.ValidateSession("").IsSuccess);
        Assert.IsFalse(auth.ValidateSession("forged-token").IsSuccess);
    }

    /// <summary>TC-NF-SEC-002 — password material stored as hash fields, not compared as plain text in User model path</summary>
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
