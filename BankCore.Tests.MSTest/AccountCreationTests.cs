using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;

namespace BankCore.Tests.MSTest;

[TestClass]
public class AccountCreationTests
{
    private Mock<IAccountRepository> _accountRepo = null!;
    private Mock<IValidationService> _validator = null!;
    private Mock<IAuditService> _audit = null!;
    private AccountService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _accountRepo = new Mock<IAccountRepository>();
        _validator = new Mock<IValidationService>();
        _audit = new Mock<IAuditService>();
        _validator.Setup(v => v.IsValidName(It.IsAny<string>())).Returns(true);
        _validator.Setup(v => v.IsValidSouthAfricanIdNumber(It.IsAny<string>())).Returns(true);
        _validator.Setup(v => v.IsValidBranchCode(It.IsAny<string>())).Returns(true);
        _service = new AccountService(_accountRepo.Object, _validator.Object, _audit.Object);
    }

    [TestCleanup]
    public void Teardown()
    {
        _accountRepo = null!;
        _validator = null!;
        _audit = null!;
        _service = null!;
    }

    [TestMethod]
    [DataRow("Savings", 100.0)]
    [DataRow("Current", 500.0)]
    [DataRow("FixedDeposit", 1000.0)]
    [DataRow("Notice", 500.0)]
    [TestCategory("Functional")]
    public void CreateAccount_AtTypeMinimumDeposit_Succeeds(string typeName, double amount)
    {
        var type = Enum.Parse<AccountType>(typeName);
        var result = _service.CreateAccount("Alice Smith", TestDataHelper.ValidIdNumber,
            type, (decimal)amount, TestDataHelper.ValidBranchCode);

        Assert.IsTrue(result.IsSuccess, result.Message);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(type, result.Data!.Type);
        Assert.AreEqual((decimal)amount, result.Data.Balance);
        _accountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Once);
    }

    [TestMethod]
    [DataRow("Savings", 99.99, 100.00)]
    [DataRow("Current", 499.99, 500.00)]
    [DataRow("FixedDeposit", 999.99, 1000.00)]
    [DataRow("Notice", 499.99, 500.00)]
    [TestCategory("Boundary")]
    [TestCategory("Negative")]
    public void CreateAccount_JustBelowTypeMinimum_Fails(string typeName, double amount, double minimum)
    {
        var type = Enum.Parse<AccountType>(typeName);
        var result = _service.CreateAccount("Alice Smith", TestDataHelper.ValidIdNumber,
            type, (decimal)amount, TestDataHelper.ValidBranchCode);

        var expectedMessage = $"Minimum opening deposit for {type} account is R{(decimal)minimum:F2}.";
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(expectedMessage, result.Message);
        _accountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Smoke")]
    public void CreateAccount_ValidInput_ReturnsActiveAccount()
    {
        var result = _service.CreateAccount(TestDataHelper.ValidOwnerName, TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(AccountStatus.Active, result.Data!.Status);
        Assert.AreEqual(TestDataHelper.ValidOwnerName, result.Data.OwnerName);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_GeneratesUniqueAccountNumbers()
    {
        var first = _service.CreateAccount("User One", TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);
        var second = _service.CreateAccount("User Two", TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsTrue(first.IsSuccess && second.IsSuccess);
        Assert.StartsWith(first.Data!.AccountNumber, "BC1000000005");
        Assert.AreNotEqual(first.Data.AccountNumber, second.Data!.AccountNumber);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_SetsDailyWithdrawalLimitFromAccountType()
    {
        var result = _service.CreateAccount(TestDataHelper.ValidOwnerName, TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(5000m, result.Data!.DailyWithdrawalLimit);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CreateAccount_InvalidOwnerName_DoesNotPersist()
    {
        _validator.Setup(v => v.IsValidName("")).Returns(false);
        var result = _service.CreateAccount("", TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsFalse(result.IsSuccess);
        _accountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CreateAccount_InvalidBranchCode_DoesNotPersist()
    {
        _validator.Setup(v => v.IsValidBranchCode("bad")).Returns(false);
        var result = _service.CreateAccount(TestDataHelper.ValidOwnerName, TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, "bad");

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "invalid branch code.");
        _accountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void CreateAccount_InvalidId_DoesNotPersist()
    {
        _validator.Setup(v => v.IsValidSouthAfricanIdNumber("bad-id")).Returns(false);
        var result = _service.CreateAccount(TestDataHelper.ValidOwnerName, "bad-id",
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsFalse(result.IsSuccess);
        Assert.Contains(result.Message.ToLowerInvariant(), "invalid south african id number.");
        _accountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_ValidInput_VerifiesAuditEvent()
    {
        var result = _service.CreateAccount(TestDataHelper.ValidOwnerName, TestDataHelper.ValidIdNumber,
            AccountType.Savings, 500m, TestDataHelper.ValidBranchCode);

        Assert.IsTrue(result.IsSuccess);
        _audit.Verify(a => a.Log(
            "ACCOUNT_CREATED", "SYSTEM", It.Is<string>(message => message.Contains("created")),
            It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }
}