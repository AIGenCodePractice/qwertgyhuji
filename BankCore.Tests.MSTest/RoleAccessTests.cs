using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// TC-AUTH-003, TC-AUTH-004, TC-AUTH-010
/// Verify Teller vs Admin authorization boundaries are enforced on protected operations.
/// </summary>
[TestClass]
public class RoleAccessTests
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

    private AuthService CreateAuth(Session session)
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

    /// <summary>TC-AUTH-003 — Teller role can access only permitted functions</summary>
    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("DEPOSIT", true)]
    [DataRow("WITHDRAW", true)]
    [DataRow("TRANSFER", true)]
    [DataRow("VIEW_OWN_ACCOUNTS", true)]
    [DataRow("CREATE_ACCOUNT", false)]
    [DataRow("CLOSE_ACCOUNT", false)]
    [DataRow("VIEW_AUDIT_LOG", false)]
    [DataRow("CREATE_USER", false)]
    public void HasPermission_Teller_OnlyPermittedOps(string operation, bool expected)
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-teller", role: UserRole.Teller);
        var auth = CreateAuth(session);

        var allowed = auth.HasPermission("tok-teller", operation);

        Assert.AreEqual(expected, allowed, $"Teller permission for {operation} expected {expected}");
    }

    /// <summary>TC-AUTH-004 — Admin role can access admin functions</summary>
    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("CREATE_ACCOUNT")]
    [DataRow("UPDATE_ACCOUNT")]
    [DataRow("CLOSE_ACCOUNT")]
    [DataRow("APPROVE_LOAN")]
    [DataRow("CREATE_USER")]
    [DataRow("LOCK_USER")]
    [DataRow("UNLOCK_USER")]
    [DataRow("VIEW_AUDIT_LOG")]
    [DataRow("GENERATE_REPORT")]
    public void HasPermission_Admin_CanAccessAdminFunctions(string operation)
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-admin", role: UserRole.Admin);
        var auth = CreateAuth(session);

        var allowed = auth.HasPermission("tok-admin", operation);

        Assert.IsTrue(allowed, $"Admin should have permission for {operation}");
    }

    /// <summary>TC-AUTH-010 — Reject Admin function call when logged in as Teller</summary>
    [TestMethod]
    [TestCategory("Negative")]
    [TestCategory("Security")]
    public void HasPermission_TellerCallingAdminOp_ReturnsFalse()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-teller2", role: UserRole.Teller);
        var auth = CreateAuth(session);

        Assert.IsFalse(auth.HasPermission("tok-teller2", "CREATE_USER"));
        Assert.IsFalse(auth.HasPermission("tok-teller2", "UNLOCK_USER"));
        Assert.IsFalse(auth.HasPermission("tok-teller2", "VIEW_AUDIT_LOG"));
        Assert.IsFalse(auth.HasPermission("tok-teller2", "CLOSE_ACCOUNT"));
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void HasPermission_UnknownToken_ReturnsFalse()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-known", role: UserRole.Admin);
        var auth = CreateAuth(session);

        Assert.IsFalse(auth.HasPermission("tok-missing", "CREATE_ACCOUNT"));
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void HasPermission_UnknownOperation_ReturnsFalse()
    {
        var session = TestDataHelper.BuildActiveSession(token: "tok-admin", role: UserRole.Admin);
        var auth = CreateAuth(session);

        Assert.IsFalse(auth.HasPermission("tok-admin", "FLY_TO_THE_MOON"));
    }
}
