using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Direct coverage tests for the two support services previously reported at 0%.
/// Exercises both positive and negative password verification paths and all
/// AuditService Log parameters, including defaults and explicit overrides.
/// </summary>
[TestClass]
public class SupportServicesCoverageTests
{
    [TestMethod]
    public void PasswordHasher_HashPassword_ReturnsHashAndSalt_ThatVerifyCorrectly()
    {
        var hasher = new PasswordHasher();

        var (hash, salt) = hasher.HashPassword("CorrectPassword!123");

        Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
        Assert.IsFalse(string.IsNullOrWhiteSpace(salt));
        Assert.IsTrue(hasher.VerifyPassword("CorrectPassword!123", hash, salt));
    }

    [TestMethod]
    public void PasswordHasher_VerifyPassword_ReturnsFalse_ForWrongPassword()
    {
        var hasher = new PasswordHasher();
        var (hash, salt) = hasher.HashPassword("CorrectPassword!123");

        var verified = hasher.VerifyPassword("WrongPassword!123", hash, salt);

        Assert.IsFalse(verified);
    }

    [TestMethod]
    public void PasswordHasher_HashPassword_GeneratesIndependentSalts()
    {
        var hasher = new PasswordHasher();

        var first = hasher.HashPassword("SamePassword!123");
        var second = hasher.HashPassword("SamePassword!123");

        Assert.AreNotEqual(first.salt, second.salt);
        Assert.AreNotEqual(first.hash, second.hash);
        Assert.IsTrue(hasher.VerifyPassword("SamePassword!123", first.hash, first.salt));
        Assert.IsTrue(hasher.VerifyPassword("SamePassword!123", second.hash, second.salt));
    }

    [TestMethod]
    public void AuditService_Log_UsesDefaultValues_AndPersistsEntry()
    {
        var repo = new Mock<IAuditRepository>();
        AuditLog? captured = null;
        repo.Setup(r => r.Add(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => captured = log);
        var service = new AuditService(repo.Object);
        var before = DateTime.UtcNow;

        service.Log("LOGIN", "anne", "Successful login");

        var after = DateTime.UtcNow;
        Assert.IsNotNull(captured);
        Assert.AreEqual("LOGIN", captured.EventType);
        Assert.AreEqual("anne", captured.Username);
        Assert.AreEqual("Successful login", captured.Description);
        Assert.IsNull(captured.RelatedReference);
        Assert.IsTrue(captured.IsSuccessful);
        Assert.AreEqual("127.0.0.1", captured.IpAddress);

        // System clock reads can have sub-millisecond jitter. Assert that the
        // timestamp was created at test execution time without requiring two
        // separate DateTime.UtcNow reads to be perfectly monotonic.
        var lowerBound = before.AddSeconds(-1);
        var upperBound = after.AddSeconds(1);
        Assert.IsGreaterThanOrEqualTo(captured.Timestamp, lowerBound);
        Assert.IsLessThanOrEqualTo(captured.Timestamp, upperBound);
        repo.Verify(r => r.Add(It.IsAny<AuditLog>()), Times.Once);
    }

    [TestMethod]
    public void AuditService_Log_UsesExplicitOptionalValues()
    {
        var repo = new Mock<IAuditRepository>();
        AuditLog? captured = null;
        repo.Setup(r => r.Add(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(log => captured = log);
        var service = new AuditService(repo.Object);

        service.Log("TRANSFER", "teller1", "Transfer declined", "TXN-100", false, "10.10.10.10");

        Assert.IsNotNull(captured);
        Assert.AreEqual("TRANSFER", captured.EventType);
        Assert.AreEqual("teller1", captured.Username);
        Assert.AreEqual("Transfer declined", captured.Description);
        Assert.AreEqual("TXN-100", captured.RelatedReference);
        Assert.IsFalse(captured.IsSuccessful);
        Assert.AreEqual("10.10.10.10", captured.IpAddress);
        repo.Verify(r => r.Add(It.Is<AuditLog>(log =>
            log.EventType == "TRANSFER" &&
            log.Username == "teller1" &&
            log.RelatedReference == "TXN-100" &&
            !log.IsSuccessful &&
            log.IpAddress == "10.10.10.10")), Times.Once);
    }
}
