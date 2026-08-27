using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>TC-REP-003 — audit log for a known transaction</summary>
[TestFixture]
public class AuditTrailTests
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
        var account = new Account { Id = 1, Status = AccountStatus.Active, Balance = 1000m, AccountNumber = "BC1000000001", DailyWithdrawalLimit = 5000m };
        _accountRepo.Setup(r => r.GetById(1)).Returns(account);
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _txnRepo.Setup(r => r.Add(It.IsAny<Transaction>()));
        _txnRepo.Setup(r => r.ReferenceExists(It.IsAny<string>())).Returns(false);
        _svc = new TransactionService(_accountRepo.Object, _txnRepo.Object, _validator.Object, _audit.Object);
    }

    [Test]
    [Category("Critical")]
    public void Deposit_ProducesAuditLogEntry()
    {
        var result = _svc.Deposit(1, 100m, "audit me", "teller1");
        Assert.That(result.IsSuccess, Is.True, result.Message);

        _audit.Verify(a => a.Log(
            "DEPOSIT",
            "teller1",
            It.Is<string>(d => d.Contains("100")),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Once);
    }
}
