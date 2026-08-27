using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Moq;

namespace BankCore.Tests.NUnit;

/// <summary>
/// Phase 3 dependency-isolation tests. Exercises real TransactionService behavior
/// while the repository dependency is configured to simulate a database failure.
/// </summary>
[TestFixture]
public class MockingFailureTests
{
    [Test]
    public void Deposit_WhenTransactionRepositoryFails_PropagatesConfiguredException()
    {
        var account = new Account
        {
            Id = 1,
            AccountNumber = "BC1000000001",
            OwnerName = "Test Owner",
            Type = AccountType.Savings,
            Status = AccountStatus.Active,
            Balance = 1000m,
            DailyWithdrawalLimit = 5000m
        };

        var accountRepo = new Mock<IAccountRepository>();
        var transactionRepo = new Mock<ITransactionRepository>();
        var validator = new Mock<IValidationService>();
        var audit = new Mock<IAuditService>();

        accountRepo.Setup(r => r.GetById(1)).Returns(account);
        transactionRepo
            .Setup(r => r.Add(It.IsAny<Transaction>()))
            .Throws(new InvalidOperationException("Database unavailable"));

        var service = new TransactionService(
            accountRepo.Object,
            transactionRepo.Object,
            validator.Object,
            audit.Object);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.Deposit(1, 100m, "Repository failure", "teller1"));

        Assert.That(exception!.Message, Is.EqualTo("Database unavailable"));
        transactionRepo.Verify(r => r.Add(It.Is<Transaction>(t =>
            t.AccountId == 1 &&
            t.Amount == 100m &&
            t.Type == TransactionType.Deposit)), Times.Once);
        audit.Verify(a => a.Log(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Never);
    }
}
