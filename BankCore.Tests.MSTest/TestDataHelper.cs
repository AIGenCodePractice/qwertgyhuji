using BankCore.Core.Models;
using BankCore.Tests.MSTest.Helpers;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Shared MSTest builder/factory utility. Produces valid and invalid fixtures so test classes
/// do not duplicate setup logic. Passwords are not hardcoded in test methods — they are read
/// from this helper (simulating configuration / TestContext-style centralisation).
/// </summary>
public static class TestDataHelper
{
    // Centralised credentials — tests must not hardcode passwords inline.
    public const string ValidPassword = "SecureP@ss1";
    public const string WeakPassword = "weak";
    public const string MinLengthPassword = "Ab1!xxxx"; // exactly 8 chars, meets complexity
    public const string BelowMinPassword = "Ab1!xxx";  // 7 chars

    public const string ValidOwnerName = "Thabo Molefe";
    public const string ValidIdNumber = "9001015800085";
    public const string ValidBranchCode = "250655";
    public const string ValidEmail = "thabo.molefe@example.co.za";
    public const string ValidPhone = "0821234567";

    public static Account BuildAccount(
        int id = 1,
        decimal balance = 1000m,
        AccountStatus status = AccountStatus.Active,
        AccountType type = AccountType.Savings,
        string? ownerName = null,
        string? ownerIdNumber = null)
    {
        return TestMockFactory.CreateSampleAccount(
            id: id,
            balance: balance,
            status: status,
            type: type,
            ownerName: ownerName ?? ValidOwnerName,
            ownerIdNumber: ownerIdNumber ?? ValidIdNumber);
    }

    public static Account BuildClosedAccount(int id = 2, decimal balance = 0m)
        => BuildAccount(id: id, balance: balance, status: AccountStatus.Closed);

    public static Account BuildDormantAccount(int id = 3, decimal balance = 500m)
        => BuildAccount(id: id, balance: balance, status: AccountStatus.Dormant);

    public static Account BuildZeroBalanceAccount(int id = 4)
        => BuildAccount(id: id, balance: 0m, status: AccountStatus.Active);

    public static User BuildUser(
        int id = 1,
        string username = "jdoe",
        UserRole role = UserRole.Teller,
        bool isLocked = false,
        int failedAttempts = 0)
        => TestMockFactory.CreateSampleUser(id, username, role, isLocked, failedAttempts);

    public static User BuildAdminUser(int id = 10, string username = "admin1")
        => BuildUser(id, username, UserRole.Admin);

    public static User BuildLockedUser(int id = 11, string username = "locked_user")
        => BuildUser(id, username, UserRole.Teller, isLocked: true, failedAttempts: 3);

    public static Session BuildActiveSession(
        string token = "tok-active-001",
        int userId = 1,
        string username = "jdoe",
        UserRole role = UserRole.Teller)
        => TestMockFactory.CreateSampleSession(token, userId, username, role, isActive: true, expiresInMinutes: 60);

    public static Session BuildExpiredSession(
        string token = "tok-expired-001",
        int userId = 1,
        string username = "jdoe")
        => TestMockFactory.CreateSampleSession(token, userId, username, UserRole.Teller, isActive: true, expiresInMinutes: -1);

    public static Session BuildInactiveSession(
        string token = "tok-inactive-001",
        int userId = 1)
        => TestMockFactory.CreateSampleSession(token, userId, "jdoe", UserRole.Teller, isActive: false, expiresInMinutes: 60);

    public static IEnumerable<object[]> ValidDepositAmounts()
    {
        yield return new object[] { 100m };
        yield return new object[] { 100.01m };
        yield return new object[] { 5000m };
        yield return new object[] { 999999.98m };
    }

    public static IEnumerable<object[]> InvalidDepositAmounts()
    {
        yield return new object[] { -1m };
        yield return new object[] { 0m };
        yield return new object[] { 99.99m };
    }
}
