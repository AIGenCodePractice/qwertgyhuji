using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

/// <summary>
/// TC-ACCT-016, TC-ACCT-018
/// Verify single-account and per-customer account lookups return correct data.
/// </summary>
[TestClass]
public class AccountQueryTests
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

    /// <summary>TC-ACCT-016 — Query account by valid account number</summary>
    [TestMethod]
    [TestCategory("Smoke")]
    [TestCategory("Functional")]
    public void GetAccountByNumber_ValidNumber_ReturnsAccount()
    {
        var existing = TestDataHelper.BuildAccount(id: 1);
        existing.AccountNumber = "BC1000000001";
        _mockRepo = TestMockFactory.CreateAccountRepositoryWithAccount(existing);
        _service = new AccountService(_mockRepo.Object, _mockValidator!.Object, _mockAudit!.Object);

        var result = _service.GetAccountByNumber("BC1000000001");

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("BC1000000001", result.Data.AccountNumber);
        Assert.AreEqual(existing.OwnerName, result.Data.OwnerName);
    }

    /// <summary>TC-ACCT-018 — List all accounts for a selected Customer ID</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void GetAccountsByOwner_ValidId_ReturnsList()
    {
        var a1 = TestDataHelper.BuildAccount(id: 1, ownerIdNumber: TestDataHelper.ValidIdNumber);
        var a2 = TestDataHelper.BuildAccount(id: 2, balance: 200m, ownerIdNumber: TestDataHelper.ValidIdNumber);
        a2.AccountNumber = "BC1000000002";

        _mockRepo = new Mock<IAccountRepository>();
        _mockRepo.Setup(r => r.GetByOwnerIdNumber(TestDataHelper.ValidIdNumber))
            .Returns(new List<Account> { a1, a2 });
        _service = new AccountService(_mockRepo.Object, _mockValidator!.Object, _mockAudit!.Object);

        var result = _service.GetAccountsByOwner(TestDataHelper.ValidIdNumber);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.HasCount(2, result.Data);
        Assert.IsTrue(result.Data.Exists(a => a.Id == 1));
        Assert.IsTrue(result.Data.Exists(a => a.Id == 2));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void GetAccount_ById_ReturnsAccount()
    {
        var existing = TestDataHelper.BuildAccount(id: 7);
        _mockRepo = TestMockFactory.CreateAccountRepositoryWithAccount(existing);
        _service = new AccountService(_mockRepo.Object, _mockValidator!.Object, _mockAudit!.Object);

        var result = _service.GetAccount(7);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(7, result.Data!.Id);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void GetAccount_NonExistent_ReturnsFailure()
    {
        var existing = TestDataHelper.BuildAccount(id: 1);
        _mockRepo = TestMockFactory.CreateAccountRepositoryWithAccount(existing);
        _service = new AccountService(_mockRepo.Object, _mockValidator!.Object, _mockAudit!.Object);

        var result = _service.GetAccount(999);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message, "Account not found.");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void GetAccountByNumber_InvalidFormat_ReturnsFailure()
    {
        _mockValidator!.Setup(v => v.IsValidAccountNumber("BAD")).Returns(false);
        _mockRepo = TestMockFactory.CreateAccountRepository();
        _service = new AccountService(_mockRepo.Object, _mockValidator.Object, _mockAudit!.Object);

        var result = _service.GetAccountByNumber("BAD");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "invalid account number format.");
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void GetAccountsByOwner_InvalidId_ReturnsFailure()
    {
        _mockValidator!.Setup(v => v.IsValidSouthAfricanIdNumber("123")).Returns(false);
        _mockRepo = TestMockFactory.CreateAccountRepository();
        _service = new AccountService(_mockRepo.Object, _mockValidator.Object, _mockAudit!.Object);

        var result = _service.GetAccountsByOwner("123");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "invalid id number.");
    }
}
