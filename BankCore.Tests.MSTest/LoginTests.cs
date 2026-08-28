using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

[TestClass]
public class LoginTests
{
    private Mock<IUserRepository>? _mockUsers;
    private Mock<ISessionRepository>? _mockSessions;
    private Mock<IPasswordHasher>? _mockHasher;
    private Mock<IAuditService>? _mockAudit;
    private Mock<IValidationService>? _mockValidator;
    private AuthService? _auth;

    [TestInitialize]
    public void Setup()
    {
        _mockSessions = TestMockFactory.CreateSessionRepository();
        _mockAudit = TestMockFactory.CreateAuditService();
        _mockValidator = TestMockFactory.CreateValidationService(alwaysValid: true);
    }

    [TestCleanup]
    public void Teardown()
    {
        _mockUsers = null;
        _mockSessions = null;
        _mockHasher = null;
        _mockAudit = null;
        _mockValidator = null;
        _auth = null;
    }

    private AuthService CreateAuth(User user, string correctPassword)
    {
        _mockUsers = TestMockFactory.CreateUserRepositoryWithUser(user);
        _mockHasher = TestMockFactory.CreatePasswordHasher(correctPassword);
        _auth = new AuthService(_mockUsers.Object, _mockSessions!.Object, _mockHasher.Object, _mockAudit!.Object, _mockValidator!.Object);
        return _auth;
    }

    [TestMethod]
    [TestCategory("Smoke")]
    [TestCategory("Functional")]
    public void Login_ValidCredentials_ReturnsSessionToken()
    {
        var user = TestDataHelper.BuildUser(username: "jdoe");
        var auth = CreateAuth(user, TestDataHelper.ValidPassword);
        var result = auth.Login("jdoe", TestDataHelper.ValidPassword);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Data.Token));
        Assert.AreEqual("jdoe", result.Data.Username);
        Assert.IsTrue(result.Data.IsActive);
        _mockSessions!.Verify(s => s.Add(It.IsAny<Session>()), Times.Once);
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void Login_WrongCasePassword_IsRejected()
    {
        var auth = CreateAuth(TestDataHelper.BuildUser(username: "jdoe"), TestDataHelper.ValidPassword);
        var wrongCase = TestDataHelper.ValidPassword.ToLowerInvariant();
        Assert.AreNotEqual(TestDataHelper.ValidPassword, wrongCase);
        var result = auth.Login("jdoe", wrongCase);
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "invalid username or password.");
        _mockSessions!.Verify(s => s.Add(It.IsAny<Session>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void UnlockThenLogin_Succeeds()
    {
        var user = TestDataHelper.BuildLockedUser(username: "locked_user");
        var auth = CreateAuth(user, TestDataHelper.ValidPassword);
        var lockedResult = auth.Login("locked_user", TestDataHelper.ValidPassword);
        Assert.IsFalse(lockedResult.IsSuccess);
        StringAssert.Contains(lockedResult.Message.ToLowerInvariant(), "account is locked. contact your administrator.");
        var unlock = auth.UnlockUser("locked_user", "admin1");
        Assert.IsTrue(unlock.IsSuccess);
        Assert.IsFalse(user.IsLocked);
        Assert.AreEqual(0, user.FailedLoginAttempts);
        var result = auth.Login("locked_user", TestDataHelper.ValidPassword);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void Login_IncorrectPassword_ReturnsFailure()
    {
        var auth = CreateAuth(TestDataHelper.BuildUser(username: "jdoe"), TestDataHelper.ValidPassword);
        var result = auth.Login("jdoe", "WrongP@ss99");
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "invalid username or password.");
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Once);
        _mockSessions!.Verify(s => s.Add(It.IsAny<Session>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void Login_NonExistentUsername_ReturnsFailure()
    {
        var auth = CreateAuth(TestDataHelper.BuildUser(username: "jdoe"), TestDataHelper.ValidPassword);
        var result = auth.Login("nobody", TestDataHelper.ValidPassword);
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "invalid username or password.");
        _mockSessions!.Verify(s => s.Add(It.IsAny<Session>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void Login_LockedAccount_ReturnsFailure()
    {
        var auth = CreateAuth(TestDataHelper.BuildLockedUser(username: "locked_user"), TestDataHelper.ValidPassword);
        var result = auth.Login("locked_user", TestDataHelper.ValidPassword);
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "account is locked. contact your administrator.");
        _mockSessions!.Verify(s => s.Add(It.IsAny<Session>()), Times.Never);
    }

    [DataTestMethod]
    [TestCategory("Negative")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void Login_EmptyUsernameOrPassword_ReturnsFailure(string? blank)
    {
        var auth = CreateAuth(TestDataHelper.BuildUser(), TestDataHelper.ValidPassword);
        var r1 = auth.Login(blank!, TestDataHelper.ValidPassword);
        var r2 = auth.Login("jdoe", blank!);
        Assert.IsFalse(r1.IsSuccess);
        Assert.IsFalse(r2.IsSuccess);
    }
}
