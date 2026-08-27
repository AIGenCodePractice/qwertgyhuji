using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BankCore.Tests.MSTest;

[TestClass]
public class AuthBranchCoverageTests
{
    private Mock<IUserRepository> _users = null!;
    private Mock<ISessionRepository> _sessions = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private Mock<IAuditService> _audit = null!;
    private Mock<IValidationService> _validator = null!;
    private AuthService _auth = null!;

    [TestInitialize]
    public void Setup()
    {
        _users = new Mock<IUserRepository>();
        _sessions = new Mock<ISessionRepository>();
        _hasher = new Mock<IPasswordHasher>();
        _audit = new Mock<IAuditService>();
        _validator = new Mock<IValidationService>();
        _auth = new AuthService(_users.Object, _sessions.Object, _hasher.Object, _audit.Object, _validator.Object);
    }

    private static User User(string name = "user1", UserRole role = UserRole.Teller) => new()
    {
        Id = 7, Username = name, PasswordHash = "oldhash", Salt = "salt", Role = role,
        PasswordHistory = new List<string>()
    };

    [TestMethod]
    public void Login_ExpiredLockout_IsClearedThenLoginSucceeds()
    {
        var user = User();
        user.IsLocked = true;
        user.FailedLoginAttempts = 3;
        user.LockoutExpiry = DateTime.UtcNow.AddMinutes(-1);
        _users.Setup(r => r.GetByUsername(user.Username)).Returns(user);
        _hasher.Setup(h => h.VerifyPassword("GoodP@ss1", user.PasswordHash, user.Salt)).Returns(true);

        var result = _auth.Login(user.Username, "GoodP@ss1");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(user.IsLocked);
        Assert.AreEqual(0, user.FailedLoginAttempts);
        _users.Verify(r => r.Update(user), Times.AtLeast(2));
    }

    [TestMethod]
    public void Login_ThirdFailure_LocksAccount()
    {
        var user = User();
        user.FailedLoginAttempts = 2;
        _users.Setup(r => r.GetByUsername(user.Username)).Returns(user);
        _hasher.Setup(h => h.VerifyPassword(It.IsAny<string>(), user.PasswordHash, user.Salt)).Returns(false);

        var result = _auth.Login(user.Username, "bad");

        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(user.IsLocked);
        Assert.IsNotNull(user.LockoutExpiry);
    }

    [TestMethod]
    public void ValidateSession_CoversBlankMissingInactiveAndExpired()
    {
        Assert.IsFalse(_auth.ValidateSession(" ").IsSuccess);

        _sessions.Setup(r => r.GetByToken("missing")).Returns((Session?)null);
        Assert.IsFalse(_auth.ValidateSession("missing").IsSuccess);

        _sessions.Setup(r => r.GetByToken("inactive")).Returns(new Session { Token = "inactive", IsActive = false, ExpiresAt = DateTime.UtcNow.AddHours(1) });
        Assert.IsFalse(_auth.ValidateSession("inactive").IsSuccess);

        _sessions.Setup(r => r.GetByToken("expired")).Returns(new Session { Token = "expired", IsActive = true, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        Assert.IsFalse(_auth.ValidateSession("expired").IsSuccess);
    }

    [TestMethod]
    public void Logout_MissingAndValidSession_CoversBothBranches()
    {
        _sessions.Setup(r => r.GetByToken("missing")).Returns((Session?)null);
        Assert.IsFalse(_auth.Logout("missing").IsSuccess);

        var session = new Session { Token = "ok", Username = "user1", IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1) };
        _sessions.Setup(r => r.GetByToken("ok")).Returns(session);
        var result = _auth.Logout("ok");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(session.IsActive);
    }

    [TestMethod]
    public void ChangePassword_CoversValidationAndHistoryBranches()
    {
        _sessions.Setup(r => r.GetByToken("token")).Returns(new Session { Token = "token", UserId = 7, IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1) });
        _users.Setup(r => r.GetById(7)).Returns(User());
        _hasher.Setup(h => h.VerifyPassword("current", "oldhash", "salt")).Returns(true);

        _validator.Setup(v => v.IsValidPassword("weak")).Returns(false);
        Assert.IsFalse(_auth.ChangePassword("token", "current", "weak").IsSuccess);

        _validator.Setup(v => v.IsValidPassword("GoodP@ss2")).Returns(true);
        _hasher.Setup(h => h.HashPassword("GoodP@ss2")).Returns(("oldhash", "newsalt"));
        var reusedPasswordUser = User();
        reusedPasswordUser.PasswordHistory = new List<string> { "oldhash" };
        _users.Setup(r => r.GetById(7)).Returns(reusedPasswordUser);
        Assert.IsFalse(_auth.ChangePassword("token", "current", "GoodP@ss2").IsSuccess);

        _hasher.Setup(h => h.HashPassword("GoodP@ss2")).Returns(("newhash", "newsalt"));
        var user = User();
        user.PasswordHistory = new List<string> { "a", "b", "c", "d", "e" };
        _users.Setup(r => r.GetById(7)).Returns(user);
        var result = _auth.ChangePassword("token", "current", "GoodP@ss2");
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(5, user.PasswordHistory.Count);
    }

    [TestMethod]
    public void RegisterUser_CoversInvalidDuplicateAndSuccess()
    {
        _validator.Setup(v => v.IsValidUsername("bad")).Returns(false);
        Assert.IsFalse(_auth.RegisterUser("bad", "P@ssword1", UserRole.Teller, "admin").IsSuccess);

        _validator.Setup(v => v.IsValidUsername("gooduser")).Returns(true);
        _validator.Setup(v => v.IsValidPassword("weak")).Returns(false);
        Assert.IsFalse(_auth.RegisterUser("gooduser", "weak", UserRole.Teller, "admin").IsSuccess);

        _validator.Setup(v => v.IsValidPassword("P@ssword1")).Returns(true);
        _users.Setup(r => r.UsernameExists("gooduser")).Returns(true);
        Assert.IsFalse(_auth.RegisterUser("gooduser", "P@ssword1", UserRole.Teller, "admin").IsSuccess);

        _users.Setup(r => r.UsernameExists("gooduser")).Returns(false);
        _hasher.Setup(h => h.HashPassword("P@ssword1")).Returns(("hash", "salt"));
        Assert.IsTrue(_auth.RegisterUser("gooduser", "P@ssword1", UserRole.Teller, "admin").IsSuccess);
    }

    [TestMethod]
    public void LockUnlockAndPermissions_CoverMissingAndRoleBranches()
    {
        _users.Setup(r => r.GetByUsername("missing")).Returns((User?)null);
        Assert.IsFalse(_auth.LockUser("missing", "admin").IsSuccess);
        Assert.IsFalse(_auth.UnlockUser("missing", "admin").IsSuccess);

        var user = User("user1", UserRole.Teller);
        _users.Setup(r => r.GetByUsername("user1")).Returns(user);
        Assert.IsTrue(_auth.LockUser("user1", "admin").IsSuccess);
        Assert.IsTrue(user.IsLocked);
        Assert.IsTrue(_auth.UnlockUser("user1", "admin").IsSuccess);
        Assert.IsFalse(user.IsLocked);

        _sessions.Setup(r => r.GetByToken("none")).Returns((Session?)null);
        Assert.IsFalse(_auth.HasPermission("none", "DEPOSIT"));
        _sessions.Setup(r => r.GetByToken("inactive")).Returns(new Session { IsActive = false, ExpiresAt = DateTime.UtcNow.AddHours(1) });
        Assert.IsFalse(_auth.HasPermission("inactive", "DEPOSIT"));
        _sessions.Setup(r => r.GetByToken("expired")).Returns(new Session { IsActive = true, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        Assert.IsFalse(_auth.HasPermission("expired", "DEPOSIT"));
        _sessions.Setup(r => r.GetByToken("ok")).Returns(new Session { IsActive = true, ExpiresAt = DateTime.UtcNow.AddHours(1), Role = UserRole.Teller });
        Assert.IsTrue(_auth.HasPermission("ok", "DEPOSIT"));
        Assert.IsFalse(_auth.HasPermission("ok", "APPROVE_LOAN"));
    }
}