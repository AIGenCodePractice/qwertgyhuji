using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>
/// Withdrawal decision table derived from TransactionService.Withdraw.
/// The three conditions are sufficient funds, Active account status, and
/// whether the withdrawal remains within the configured daily limit.
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
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    private void Seed(AccountStatus status, decimal balance, decimal dailyUsed, decimal dailyLimit)
    {
        var account = new Account
        {
            Id = 1,
            Status = status,
            Balance = balance,
            DailyWithdrawnToday = dailyUsed,
            DailyWithdrawalLimit = dailyLimit
        };

        _accountRepo.Setup(r => r.GetById(1)).Returns(account);
    }

    /// <summary>
    /// Complete 2^3 decision table for the three independently modelled rules.
    /// Each TestCase is one decision-table column rather than a mechanically
    /// repeated scenario.
    /// </summary>
    [Test]
    [Category("Critical")]
    [TestCase(AccountStatus.Active, 500d, 0d, 1000d, 100d, true,
        TestName = "R1_Funds_Active_UnderLimit_APPROVE")]
    [TestCase(AccountStatus.Active, 500d, 1000d, 1000d, 100d, false,
        TestName = "R2_Funds_Active_OverLimit_REJECT")]
    [TestCase(AccountStatus.Closed, 500d, 0d, 1000d, 100d, false,
        TestName = "R3_Funds_NotActive_UnderLimit_REJECT")]
    [TestCase(AccountStatus.Closed, 500d, 1000d, 1000d, 100d, false,
        TestName = "R4_Funds_NotActive_OverLimit_REJECT")]
    [TestCase(AccountStatus.Active, 50d, 0d, 1000d, 100d, false,
        TestName = "R5_NoFunds_Active_UnderLimit_REJECT")]
    [TestCase(AccountStatus.Active, 50d, 1000d, 1000d, 100d, false,
        TestName = "R6_NoFunds_Active_OverLimit_REJECT")]
    [TestCase(AccountStatus.Dormant, 50d, 0d, 1000d, 100d, false,
        TestName = "R7_NoFunds_NotActive_UnderLimit_REJECT")]
    [TestCase(AccountStatus.Closed, 0d, 5000d, 100d, 100d, false,
        TestName = "R8_NoFunds_NotActive_OverLimit_REJECT")]
    public void Withdraw_DecisionTable_AllRuleCombinations(
        AccountStatus status,
        decimal balance,
        decimal dailyUsed,
        decimal dailyLimit,
        decimal amount,
        bool expectedSuccess)
    {
        Seed(status, balance, dailyUsed, dailyLimit);

        var result = _svc.Withdraw(1, amount, "decision-table", "tester");

        Assert.That(result.IsSuccess, Is.EqualTo(expectedSuccess), result.Message);
    }
}
