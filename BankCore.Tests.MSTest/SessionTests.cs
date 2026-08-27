using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

[TestClass]
public class SessionTests
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
        _mockUsers = TestMockFactory.CreateUserRepositoryWithUser(TestDataHelper.BuildUser());
        _mockHasher = TestMockFactory.CreatePasswordHasher(TestDataHelper.ValidPassword);
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

    private AuthService CreateAuthWithSession(Session session)
    {
        _mockSessions = TestMockFactory.CreateSessionRepository(session);
        _mockSessions.Setup(r => r.GetByToken(session.Token)).Returns(session);
        _mockSessions.Setup(r => r.GetByToken(It.Is<string>(t => t != session.Token)))
            .Returns((Session?)null);

        _auth = new AuthService(
            _mockUsers!.Object,
            _mockSessions.Object,
            _mockHasher!.Object,
            _mockAudit!.Object,
            _mockValidator!.Object);
        return _auth;
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void Logout_ValidToken_InvalidatesSession()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-logout-001");
        var auth = CreateAuthWithSession(session);

        var result = auth.Logout(session.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(session.IsActive);
        _mockSessions!.Verify(s => s.Update(It.Is<Session>(x => !x.IsActive)), Times.Once);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void ValidateSession_InactiveToken_ReturnsFailure()
    {
        var session = TestDataHelper.BuildInactiveSession(token: "tok-inactive-001");
        var auth = CreateAuthWithSession(session);

        var result = auth.ValidateSession(session.Token);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "inactive");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void ValidateSession_ExpiredToken_ReturnsFailure()
    {
        var session = TestDataHelper.BuildExpiredSession(token: "tok-expired-001");
        var auth = CreateAuthWithSession(session);

        var result = auth.ValidateSession(session.Token);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "expired");
    }

    [TestMethod]
    [TestCategory("Boundary")]
    public void ValidateSession_AtExpiryBoundary_ReturnsFailure()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-boundary-001");
        session.ExpiresAt = DateTime.UtcNow;
        var auth = CreateAuthWithSession(session);

        var result = auth.ValidateSession(session.Token);

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "expired");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void Logout_ThenValidate_ReturnsFailure()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-reuse-001");
        var auth = CreateAuthWithSession(session);

        var logout = auth.Logout(session.Token);
        var validate = auth.ValidateSession(session.Token);

        Assert.IsTrue(logout.IsSuccess);
        Assert.IsFalse(validate.IsSuccess);
        StringAssert.Contains(validate.Message.ToLowerInvariant(), "inactive");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void ValidateSession_UnknownToken_ReturnsFailure()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-known");
        var auth = CreateAuthWithSession(session);

        var result = auth.ValidateSession("tok-unknown");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "invalid");
    }

    [TestMethod]
    [TestCategory("Negative")]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ValidateSession_BlankToken_ReturnsFailure(string? token)
    {
        var auth = CreateAuthWithSession(TestDataHelper.BuildActiveSession());
        var result = auth.ValidateSession(token!);

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void Logout_UnknownToken_ReturnsFailure()
    {
        var auth = CreateAuthWithSession(TestDataHelper.BuildActiveSession(token: "tok-known"));

        var result = auth.Logout("tok-missing");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "not found");
    }
}
