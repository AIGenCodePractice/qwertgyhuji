using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// TC-AUTH-012, TC-AUTH-013, TC-AUTH-014, TC-AUTH-015
/// Verify password complexity/length rules (including exact boundary lengths)
/// and correct-old-password requirement on change.
/// Passwords sourced from TestDataHelper only.
/// </summary>
[TestClass]
public class PasswordPolicyTests
{
    private Mock<IUserRepository>? _mockUsers;
    private Mock<ISessionRepository>? _mockSessions;
    private Mock<IPasswordHasher>? _mockHasher;
    private Mock<IAuditService>? _mockAudit;
    private Mock<IValidationService>? _mockValidator;
    private AuthService? _auth;
    private User? _user;
    private Session? _session;

    [TestInitialize]
    public void Setup()
    {
        _user = TestDataHelper.BuildUser(id: 1, username: "jdoe");
        _session = TestDataHelper.BuildActiveSession(token: "tok-pwd-001", userId: 1, username: "jdoe");

        _mockUsers = TestMockFactory.CreateUserRepositoryWithUser(_user);
        _mockSessions = new Mock<ISessionRepository>();
        _mockSessions.Setup(r => r.GetByToken(_session.Token)).Returns(_session);
        _mockSessions.Setup(r => r.GetByToken(It.Is<string>(tok => tok != _session.Token)))
            .Returns((Session?)null);
        _mockSessions.Setup(r => r.Add(It.IsAny<Session>()));
        _mockSessions.Setup(r => r.Update(It.IsAny<Session>()));
        _mockHasher = TestMockFactory.CreatePasswordHasher(TestDataHelper.ValidPassword);
        _mockAudit = TestMockFactory.CreateAuditService();

        // Use real-ish password validation via mock that mirrors production rules length/complexity
        _mockValidator = new Mock<IValidationService>();
        _mockValidator.Setup(v => v.IsValidPassword(It.IsAny<string>()))
            .Returns((string p) =>
            {
                if (string.IsNullOrWhiteSpace(p) || p.Length < 8) return false;
                bool hasUpper = p.Any(char.IsUpper);
                bool hasLower = p.Any(char.IsLower);
                bool hasDigit = p.Any(char.IsDigit);
                bool hasSpecial = p.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
                return hasUpper && hasLower && hasDigit && hasSpecial;
            });

        _auth = new AuthService(
            _mockUsers.Object,
            _mockSessions.Object,
            _mockHasher.Object,
            _mockAudit.Object,
            _mockValidator.Object);
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
        _user = null;
        _session = null;
    }

    /// <summary>TC-AUTH-012 — Reject password change to a weak new password</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void ChangePassword_WeakNewPassword_ReturnsFailure()
    {
        var result = _auth!.ChangePassword(
            "tok-pwd-001",
            TestDataHelper.ValidPassword,
            TestDataHelper.WeakPassword);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "complexity");
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    /// <summary>TC-AUTH-013 — Reject password change when old password is incorrect</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void ChangePassword_IncorrectOldPassword_ReturnsFailure()
    {
        var result = _auth!.ChangePassword(
            "tok-pwd-001",
            "NotTheOldP@ss1",
            TestDataHelper.MinLengthPassword);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "incorrect");
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    /// <summary>TC-AUTH-014 — Accept password change at exact minimum length</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    [TestCategory("Functional")]
    public void ChangePassword_ExactMinimumLength_Succeeds()
    {
        Assert.AreEqual(8, TestDataHelper.MinLengthPassword.Length);

        var result = _auth!.ChangePassword(
            "tok-pwd-001",
            TestDataHelper.ValidPassword,
            TestDataHelper.MinLengthPassword);

        Assert.IsTrue(result.IsSuccess, result.Message);
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Once);
        _mockHasher!.Verify(h => h.HashPassword(TestDataHelper.MinLengthPassword), Times.Once);
    }

    /// <summary>TC-AUTH-015 — Reject password one character below minimum length</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    [TestCategory("Negative")]
    public void ChangePassword_OneBelowMinimumLength_ReturnsFailure()
    {
        Assert.AreEqual(7, TestDataHelper.BelowMinPassword.Length);

        var result = _auth!.ChangePassword(
            "tok-pwd-001",
            TestDataHelper.ValidPassword,
            TestDataHelper.BelowMinPassword);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "complexity");
        _mockUsers!.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void ChangePassword_InvalidSession_ReturnsFailure()
    {
        // Explicitly stub unknown token → null session (do not rely on shared Setup alone)
        const string badToken = "tok-missing-definitely-invalid";
        _mockSessions!.Setup(r => r.GetByToken(badToken)).Returns((Session?)null);
        _mockSessions.Setup(r => r.GetByToken(It.IsAny<string>()))
            .Returns((string token) => token == _session!.Token ? _session : null);

        var auth = new AuthService(
            _mockUsers!.Object,
            _mockSessions.Object,
            _mockHasher!.Object,
            _mockAudit!.Object,
            _mockValidator!.Object);

        var result = auth.ChangePassword(
            badToken,
            TestDataHelper.ValidPassword,
            TestDataHelper.MinLengthPassword);

        Assert.IsFalse(result.IsSuccess, $"Expected invalid session failure, got: {result.Message}");
        StringAssert.Contains(result.Message.ToLowerInvariant(), "session");
        _mockUsers.Verify(u => u.Update(It.IsAny<User>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("NoSpecial1")]   // missing special
    [DataRow("noupper1!")]    // missing upper
    [DataRow("NOLOWER1!")]    // missing lower
    [DataRow("NoDigits!!")]   // missing digit
    public void ChangePassword_MissingComplexityRule_ReturnsFailure(string badPassword)
    {
        var result = _auth!.ChangePassword(
            "tok-pwd-001",
            TestDataHelper.ValidPassword,
            badPassword);

        Assert.IsFalse(result.IsSuccess);
    }
}
