using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// TC-ACCT-012, TC-ACCT-014, TC-ACCT-017, TC-ACCT-020
/// Verify existing account fields update correctly and updates against non-existent accounts are rejected.
/// </summary>
[TestClass]
public class AccountUpdateTests
{
    private Mock<IAccountRepository>? _mockRepo;
    private Mock<IValidationService>? _mockValidator;
    private Mock<IAuditService>? _mockAudit;
    private AccountService? _service;

    [TestInitialize]
    public void Setup()
    {
        _mockValidator = TestMockFactory.CreateValidationService(alwaysValid: true);
        _mockAudit = TestMockFactory.CreateAuditService();
    }

    [TestCleanup]
    public void Teardown()
    {
        _mockRepo = null;
        _mockValidator = null;
        _mockAudit = null;
        _service = null;
    }

    private AccountService CreateService(Account existing)
    {
        _mockRepo = TestMockFactory.CreateAccountRepositoryWithAccount(existing);
        _service = new AccountService(_mockRepo.Object, _mockValidator!.Object, _mockAudit!.Object);
        return _service;
    }

    /// <summary>TC-ACCT-012 — Reject update of non-existent account</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void UpdateAccount_NonExistentAccount_ReturnsFailure()
    {
        var existing = TestDataHelper.BuildAccount(id: 1);
        var service = CreateService(existing);

        var result = service.UpdateAccount(999, "New Name", TestDataHelper.ValidBranchCode);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message, "not found");
        _mockRepo!.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    /// <summary>TC-ACCT-014 — Successfully update account holder name</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void UpdateAccount_ValidName_UpdatesOwnerName()
    {
        var existing = TestDataHelper.BuildAccount(id: 1, ownerName: "Old Name");
        var service = CreateService(existing);

        var result = service.UpdateAccount(1, "Thabo Molefe", TestDataHelper.ValidBranchCode);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("Thabo Molefe", result.Data.OwnerName);
        _mockRepo!.Verify(r => r.Update(It.Is<Account>(a => a.OwnerName == "Thabo Molefe")), Times.Once);
    }

    /// <summary>TC-ACCT-017 — Update contact details (branch code treated as contact/branch field)</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void UpdateAccount_ValidBranchCode_UpdatesBranch()
    {
        var existing = TestDataHelper.BuildAccount(id: 1);
        var service = CreateService(existing);
        const string newBranch = "632005";

        var result = service.UpdateAccount(1, TestDataHelper.ValidOwnerName, newBranch);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newBranch, result.Data!.BranchCode);
        _mockRepo!.Verify(r => r.Update(It.Is<Account>(a => a.BranchCode == newBranch)), Times.Once);
    }

    /// <summary>TC-ACCT-020 — Transition account from Active to Dormant after inactivity period</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void SetDormant_ActiveAccount_TransitionsToDormant()
    {
        var existing = TestDataHelper.BuildAccount(id: 1, status: AccountStatus.Active);
        var service = CreateService(existing);

        var result = service.SetDormant(1);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(AccountStatus.Dormant, existing.Status);
        _mockRepo!.Verify(r => r.Update(It.Is<Account>(a => a.Status == AccountStatus.Dormant)), Times.Once);
        _mockAudit!.Verify(a => a.Log(
            "ACCOUNT_DORMANT",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void UpdateAccount_ClosedAccount_ReturnsFailure()
    {
        var existing = TestDataHelper.BuildClosedAccount(id: 5);
        var service = CreateService(existing);

        var result = service.UpdateAccount(5, "Someone", TestDataHelper.ValidBranchCode);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "closed");
        _mockRepo!.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void UpdateAccount_InvalidName_ReturnsFailure()
    {
        var existing = TestDataHelper.BuildAccount(id: 1);
        _mockValidator!.Setup(v => v.IsValidName("")).Returns(false);
        var service = CreateService(existing);

        var result = service.UpdateAccount(1, "", TestDataHelper.ValidBranchCode);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "name");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void SetDormant_AlreadyDormant_ReturnsFailure()
    {
        var existing = TestDataHelper.BuildDormantAccount(id: 3);
        var service = CreateService(existing);

        var result = service.SetDormant(3);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "active");
    }
}
