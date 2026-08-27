using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using Moq;

namespace BankCore.Tests.MSTest.Helpers;

/// <summary>
/// Shared Moq factory for MSTest suites. Produces pre-configured mocks and sample domain objects
/// so individual test classes do not duplicate setup logic.
/// </summary>
public static class MockFactory
{
    public static Mock<IAccountRepository> CreateAccountRepository(
        Account? accountToReturn = null,
        List<Account>? allAccounts = null)
    {
        var mock = new Mock<IAccountRepository>();

        mock.Setup(r => r.GetById(It.IsAny<int>()))
            .Returns(accountToReturn);

        mock.Setup(r => r.GetByAccountNumber(It.IsAny<string>()))
            .Returns(accountToReturn);

        mock.Setup(r => r.GetAll())
            .Returns(allAccounts ?? new List<Account>());

        mock.Setup(r => r.GetByOwnerIdNumber(It.IsAny<string>()))
            .Returns(allAccounts ?? new List<Account>());

        mock.Setup(r => r.AccountNumberExists(It.IsAny<string>()))
            .Returns(false);

        mock.Setup(r => r.Add(It.IsAny<Account>()));
        mock.Setup(r => r.Update(It.IsAny<Account>()));
        mock.Setup(r => r.Delete(It.IsAny<int>()));

        return mock;
    }

    public static Mock<IAccountRepository> CreateAccountRepositoryWithAccount(Account existingAccount)
    {
        var mock = new Mock<IAccountRepository>();

        mock.Setup(r => r.GetById(existingAccount.Id))
            .Returns(existingAccount);
        mock.Setup(r => r.GetById(It.Is<int>(id => id != existingAccount.Id)))
            .Returns((Account?)null);

        mock.Setup(r => r.GetByAccountNumber(existingAccount.AccountNumber))
            .Returns(existingAccount);
        mock.Setup(r => r.GetByAccountNumber(It.Is<string>(n => n != existingAccount.AccountNumber)))
            .Returns((Account?)null);

        mock.Setup(r => r.GetByOwnerIdNumber(existingAccount.OwnerIdNumber))
            .Returns(new List<Account> { existingAccount });
        mock.Setup(r => r.GetByOwnerIdNumber(It.Is<string>(id => id != existingAccount.OwnerIdNumber)))
            .Returns(new List<Account>());

        mock.Setup(r => r.GetAll())
            .Returns(new List<Account> { existingAccount });

        mock.Setup(r => r.AccountNumberExists(It.IsAny<string>())).Returns(false);
        mock.Setup(r => r.Add(It.IsAny<Account>()));
        mock.Setup(r => r.Update(It.IsAny<Account>()));
        mock.Setup(r => r.Delete(It.IsAny<int>()));

        return mock;
    }

    public static Mock<ITransactionRepository> CreateTransactionRepository(
        Transaction? transactionToReturn = null)
    {
        var mock = new Mock<ITransactionRepository>();

        mock.Setup(r => r.GetById(It.IsAny<int>()))
            .Returns(transactionToReturn);

        mock.Setup(r => r.GetByReference(It.IsAny<string>()))
            .Returns(transactionToReturn);

        mock.Setup(r => r.GetByAccountId(It.IsAny<int>()))
            .Returns(new List<Transaction>());

        mock.Setup(r => r.ReferenceExists(It.IsAny<string>()))
            .Returns(false);

        mock.Setup(r => r.Add(It.IsAny<Transaction>()));
        mock.Setup(r => r.Update(It.IsAny<Transaction>()));

        return mock;
    }

    public static Mock<IAuditService> CreateAuditService()
    {
        var mock = new Mock<IAuditService>();

        mock.Setup(a => a.Log(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()));

        return mock;
    }

    public static Mock<IValidationService> CreateValidationService(bool alwaysValid = true)
    {
        var mock = new Mock<IValidationService>();

        mock.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()))
            .Returns(alwaysValid);
        mock.Setup(v => v.IsValidName(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidSouthAfricanIdNumber(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidBranchCode(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidAccountNumber(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsSafeInput(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidEmail(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidUsername(It.IsAny<string>())).Returns(alwaysValid);
        mock.Setup(v => v.IsValidPassword(It.IsAny<string>())).Returns(alwaysValid);

        return mock;
    }

    public static Mock<IUserRepository> CreateUserRepositoryWithUser(User existingUser)
    {
        var mock = new Mock<IUserRepository>();

        mock.Setup(r => r.GetByUsername(existingUser.Username)).Returns(existingUser);
        mock.Setup(r => r.GetByUsername(It.Is<string>(u => u != existingUser.Username)))
            .Returns((User?)null);
        mock.Setup(r => r.GetById(existingUser.Id)).Returns(existingUser);
        mock.Setup(r => r.GetById(It.Is<int>(id => id != existingUser.Id)))
            .Returns((User?)null);

        mock.Setup(r => r.UsernameExists(It.IsAny<string>())).Returns(false);
        mock.Setup(r => r.Add(It.IsAny<User>()));
        mock.Setup(r => r.Update(It.IsAny<User>()));
        mock.Setup(r => r.GetAll()).Returns(new List<User> { existingUser });

        return mock;
    }

    public static Mock<ISessionRepository> CreateSessionRepository(Session? sessionToReturn = null)
    {
        var mock = new Mock<ISessionRepository>();

        mock.Setup(r => r.Add(It.IsAny<Session>()));
        mock.Setup(r => r.Update(It.IsAny<Session>()));
        mock.Setup(r => r.GetByToken(It.IsAny<string>())).Returns(sessionToReturn);
        mock.Setup(r => r.InvalidateAllForUser(It.IsAny<int>()));

        return mock;
    }

    /// <summary>
    /// Case-sensitive exact match against <paramref name="correctPassword"/>.
    /// Makes TC-AUTH-005 meaningful instead of trivially true.
    /// </summary>
    public static Mock<IPasswordHasher> CreatePasswordHasher(string correctPassword)
    {
        var mock = new Mock<IPasswordHasher>();

        mock.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string pw, string hash, string salt) => pw == correctPassword);
        mock.Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns(("mocked-hash", "mocked-salt"));

        return mock;
    }

    public static Account CreateSampleAccount(
        int id = 1,
        decimal balance = 1000m,
        AccountStatus status = AccountStatus.Active,
        AccountType type = AccountType.Savings,
        string ownerName = "Test User",
        string ownerIdNumber = "9001015800085",
        string accountNumber = "BC1000000001",
        string branchCode = "250655")
    {
        return new Account
        {
            Id = id,
            AccountNumber = accountNumber,
            OwnerName = ownerName,
            OwnerIdNumber = ownerIdNumber,
            Type = type,
            Status = status,
            Balance = balance,
            DailyWithdrawalLimit = 5000m,
            DailyWithdrawnToday = 0m,
            DateOpened = DateTime.UtcNow.AddMonths(-6),
            LastActivityDate = DateTime.UtcNow,
            BranchCode = branchCode
        };
    }

    public static User CreateSampleUser(
        int id = 1,
        string username = "jdoe",
        UserRole role = UserRole.Teller,
        bool isLocked = false,
        int failedAttempts = 0)
    {
        return new User
        {
            Id = id,
            Username = username,
            PasswordHash = "irrelevant-mocked",
            Salt = "irrelevant-mocked",
            Role = role,
            IsLocked = isLocked,
            FailedLoginAttempts = failedAttempts,
            PasswordHistory = new List<string>()
        };
    }

    public static Session CreateSampleSession(
        string token = "tok-valid-001",
        int userId = 1,
        string username = "jdoe",
        UserRole role = UserRole.Teller,
        bool isActive = true,
        int expiresInMinutes = 60)
    {
        return new Session
        {
            Token = token,
            UserId = userId,
            Username = username,
            Role = role,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            IsActive = isActive
        };
    }
}
