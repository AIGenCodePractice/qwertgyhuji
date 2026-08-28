using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using BankCore.Tests.MSTest.Helpers;
using Moq;

namespace BankCore.Tests.MSTest;

[TestClass]
public class AccountClosureTests
{
    private Mock<IAccountRepository>? _repo;
    private Mock<IValidationService>? _validator;
    private Mock<IAuditService>? _audit;
    private AccountService? _service;

    [TestInitialize]
    public void Setup()
    {
        _validator = TestMockFactory.CreateValidationService(alwaysValid: true);
        _audit = TestMockFactory.CreateAuditService();
    }

    [TestCleanup]
    public void Teardown()
    {
        _repo = null;
        _validator = null;
        _audit = null;
        _service = null;
    }

    private AccountService CreateService(Account account)
    {
        _repo = TestMockFactory.CreateAccountRepositoryWithAccount(account);
        _service = new AccountService(_repo.Object, _validator!.Object, _audit!.Object);
        return _service;
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CloseAccount_NonZeroBalance_ReturnsFailure()
    {
        var service = CreateService(TestDataHelper.BuildAccount(balance: 250.50m));
        var result = service.CloseAccount(1, "teller1");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "account balance must be zero before closure. please withdraw remaining funds.");
        _repo!.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    [DataTestMethod]
    [TestCategory("Boundary")]
    [DataRow(0.01)]
    [DataRow(-1.00)]
    public void CloseAccount_NonZeroBalanceIncludingNegative_ReturnsFailure(double balance)
    {
        var service = CreateService(TestDataHelper.BuildAccount(balance: (decimal)balance));
        var result = service.CloseAccount(1, "teller1");

        Assert.IsFalse(result.IsSuccess);
        _repo!.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CloseAccount_AlreadyClosed_ReturnsFailure()
    {
        var service = CreateService(TestDataHelper.BuildClosedAccount(id: 2));
        var result = service.CloseAccount(2, "teller1");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "account is already closed.");
    }

    [TestMethod]
    [TestCategory("Smoke")]
    [TestCategory("Functional")]
    public void CloseAccount_ZeroBalanceActive_Succeeds()
    {
        var service = CreateService(TestDataHelper.BuildZeroBalanceAccount(id: 4));
        var result = service.CloseAccount(4, "admin1");

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(AccountStatus.Closed, result.Data!.Status);
        _repo!.Verify(r => r.Update(It.Is<Account>(a => a.Status == AccountStatus.Closed)), Times.Once);
        _audit!.Verify(a => a.Log(
            "ACCOUNT_CLOSED", "admin1", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CloseAccount_DormantZeroBalance_ReturnsFailure()
    {
        var service = CreateService(TestDataHelper.BuildDormantAccount(id: 3, balance: 0m));
        var result = service.CloseAccount(3, "teller1");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "only active accounts can be closed.");
        _repo!.Verify(r => r.Update(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void ReactivateAccount_ClosedAccount_ReturnsFailure()
    {
        var service = CreateService(TestDataHelper.BuildClosedAccount(id: 2));
        var result = service.ReactivateAccount(2, 100m);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "only dormant accounts can be reactivated.");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void ReactivateAccount_DormantWithMinimumDeposit_Succeeds()
    {
        var account = TestDataHelper.BuildDormantAccount(id: 3, balance: 100m);
        var service = CreateService(account);
        var result = service.ReactivateAccount(3, 50m);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(AccountStatus.Active, account.Status);
        Assert.AreEqual(150m, account.Balance);
    }

    [TestMethod]
    [TestCategory("Boundary")]
    public void ReactivateAccount_OneCentBelowMinimum_Fails()
    {
        var service = CreateService(TestDataHelper.BuildDormantAccount(id: 3));
        var result = service.ReactivateAccount(3, 49.99m);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message, "Reactivation requires a minimum deposit of R50.");
    }
}
