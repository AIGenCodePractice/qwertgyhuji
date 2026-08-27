using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Comprehensive test class for Account Creation using MSTest framework.
/// Demonstrates:
/// - [TestClass] and [TestMethod] attributes
/// - [DataTestMethod] with [DataRow] for parameterized tests
/// - [TestInitialize] and [TestCleanup] for setup/teardown
/// - [TestCategory] for test organization
/// - [ExpectedException] and Assert.ThrowsException
/// - CollectionAssert for list validations
/// - StringAssert for formatted output
/// - Moq for dependency mocking
/// </summary>
[TestClass]
public class AccountCreationTests
{
    private Mock<IAccountRepository> _mockAccountRepo = null!;
    private Mock<IValidationService> _mockValidator = null!;
    private Mock<IAuditService> _mockAudit = null!;
    private AccountService _accountService = null!;

    [TestInitialize]
    public void Setup()
    {
        // Initialize mocks before each test
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockValidator = new Mock<IValidationService>();
        _mockAudit = new Mock<IAuditService>();

        // Configure default behavior for validator
        _mockValidator.Setup(v => v.IsValidName(It.IsAny<string>())).Returns(true);
        _mockValidator.Setup(v => v.IsValidSouthAfricanIdNumber(It.IsAny<string>())).Returns(true);
        _mockValidator.Setup(v => v.IsValidBranchCode(It.IsAny<string>())).Returns(true);
        _mockValidator.Setup(v => v.IsValidAmount(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>())).Returns(true);

        _accountService = new AccountService(_mockAccountRepo.Object, _mockValidator.Object, _mockAudit.Object);
    }

    [TestCleanup]
    public void Teardown()
    {
        // Cleanup after each test
        _mockValidator = null;
        _mockAudit = null;
        _accountService = null;
    }

    [TestMethod]
    [TestCategory("Smoke")]
    public void CreateAccount_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        const string ownerName = "John Doe";
        const string idNumber = "1234567890123";
        const string branchCode = "BRANCH001";
        const decimal initialDeposit = 500m;

        // Act
        var result = _accountService.CreateAccount(ownerName, idNumber, AccountType.Savings, initialDeposit, branchCode);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.OwnerName.Should().Be(ownerName);
        result.Data.Type.Should().Be(AccountType.Savings);
        result.Data.Status.Should().Be(AccountStatus.Active);
        result.Data.Balance.Should().Be(initialDeposit);
    }

    [TestMethod]
    [TestCategory("Functional")]
    // DataRow cannot pass decimal/enum cleanly in all MSTest hosts — use double + int ordinals
    [DataRow("Alice Smith", "9876543210987", 100.0, 0)]   // Savings
    [DataRow("Bob Johnson", "1111111111111", 500.0, 1)]   // Current
    [DataRow("Carol White", "2222222222222", 1000.0, 2)]  // FixedDeposit
    [DataRow("David Brown", "3333333333333", 500.0, 3)]   // Notice
    public void CreateAccount_WithVariousTypes_CreateAccountCorrectly(
        string name, string idNumber, double deposit, int typeOrdinal)
    {
        var type = (AccountType)typeOrdinal;
        var depositAmt = (decimal)deposit;

        var result = _accountService.CreateAccount(name, idNumber, type, depositAmt, "250655");

        Assert.IsTrue(result.IsSuccess, $"Account creation should succeed: {result.Message}");
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(type, result.Data!.Type);
        Assert.AreEqual(depositAmt, result.Data.Balance);
        _mockAccountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "1234567890123", 500.0)]
    [DataRow("John", "INVALID", 500.0)]
    [DataRow("Jane", "1234567890123", -100.0)]
    public void CreateAccount_WithInvalidInput_ReturnsFailed(
        string name, string idNumber, double deposit)
    {
        // Configure validator per case (avoid broken Setup expressions with ternary + It.IsAny)
        _mockValidator.Setup(v => v.IsValidName(It.IsAny<string>()))
            .Returns((string n) => !string.IsNullOrWhiteSpace(n));
        _mockValidator.Setup(v => v.IsValidSouthAfricanIdNumber(It.IsAny<string>()))
            .Returns((string id) => id != "INVALID" && !string.IsNullOrWhiteSpace(id));
        _mockValidator.Setup(v => v.IsValidBranchCode(It.IsAny<string>())).Returns(true);

        var result = _accountService.CreateAccount(
            name, idNumber, AccountType.Savings, (decimal)deposit, "250655");

        Assert.IsFalse(result.IsSuccess, "Invalid input should fail");
        Assert.IsFalse(string.IsNullOrEmpty(result.Message));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_WithInsufficientInitialDeposit_ReturnsFailed()
    {
        // Arrange
        const decimal belowMinimum = 50m;  // Savings minimum is 100

        // Act
        var result = _accountService.CreateAccount("John Doe", "1234567890123", 
            AccountType.Savings, belowMinimum, "BRANCH001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message, "Minimum");
    }

    [TestMethod]
    [TestCategory("Smoke")]
    public void CreateAccount_VerifyAccountNumberFormat_IsValid()
    {
        // Arrange
        const string expectedPrefix = "BC";

        // Act
        var result = _accountService.CreateAccount("Test User", "1234567890123", 
            AccountType.Savings, 500m, "BRANCH001");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        StringAssert.StartsWith(result.Data!.AccountNumber, expectedPrefix);
        Assert.IsTrue(result.Data.AccountNumber.Length > expectedPrefix.Length);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_ValidInput_CallsRepositoryAdd_Once()
    {
        // Arrange
        _mockAccountRepo.Setup(r => r.Add(It.IsAny<Account>()));

        // Act
        _accountService.CreateAccount("John Doe", "1234567890123", AccountType.Savings, 500m, "BRANCH001");

        // Assert - Verify mock was called exactly once
        _mockAccountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_CallsAuditLog_WithCorrectParameters()
    {
        // Arrange
        const string ownerName = "John Doe";
        _mockAudit.Setup(a => a.Log(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string>()));

        // Act
        _accountService.CreateAccount(ownerName, "1234567890123", AccountType.Savings, 500m, "BRANCH001");

        // Assert - Verify audit log was called with ACCOUNT_CREATED event
        _mockAudit.Verify(a => a.Log(
            It.Is<string>(e => e == "ACCOUNT_CREATED"),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [TestCategory("Regression")]
    public void CreateAccount_MultipleAccounts_HaveUniqueAccountNumbers()
    {
        // Arrange
        var result1 = _accountService.CreateAccount("User One", "1111111111111", AccountType.Savings, 500m, "BRANCH001");
        var result2 = _accountService.CreateAccount("User Two", "2222222222222", AccountType.Savings, 500m, "BRANCH001");

        // Act & Assert
        Assert.IsTrue(result1.IsSuccess && result2.IsSuccess);
        Assert.AreNotEqual(result1.Data!.AccountNumber, result2.Data!.AccountNumber);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_RepositoryReturnsMultipleAccounts_CollectionAssert()
    {
        // Arrange
        _mockAccountRepo.Setup(r => r.GetAll()).Returns(new List<Account>
        {
            new() { AccountNumber = "BC1001", OwnerName = "User1" },
            new() { AccountNumber = "BC1002", OwnerName = "User2" }
        });

        // Act
        var accounts = _mockAccountRepo.Object.GetAll();

        // Assert - Verify collection contains expected elements
        Assert.IsTrue(accounts.Count > 0);
        CollectionAssert.AllItemsAreNotNull(accounts);
        Assert.AreEqual(2, accounts.Count);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_SuccessMessage_ContainsAccountNumber()
    {
        // Arrange & Act
        var result = _accountService.CreateAccount("John Doe", "1234567890123", 
            AccountType.Savings, 500m, "BRANCH001");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        StringAssert.Contains(result.Message, "created");
    }

    [TestMethod]
    [TestCategory("Regression")]
    [Ignore("Not yet implemented - account status validation pending")]
    public void CreateAccount_PendingStatusValidation_SkippedPendingImplementation()
    {
        // This test is marked as ignored because the pending account status feature
        // has not yet been implemented. Remove [Ignore] when feature is ready.
        Assert.Fail("Test should not run until account status validation is implemented");
    }

    [TestMethod]
    [TestCategory("Functional")]
    [TestCategory("Negative")]
    public void CreateAccount_NullIdNumber_ReturnsFailure()
    {
        // Production returns OperationResult.Failure rather than throwing on null/invalid ID.
        _mockValidator.Setup(v => v.IsValidSouthAfricanIdNumber(It.IsAny<string>()))
            .Returns(false);

        var result = _accountService.CreateAccount("John Doe", null!, AccountType.Savings, 500m, "250655");

        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message.ToLowerInvariant(), "id");
        _mockAccountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("Functional")]

    public void AssertThrowsExactly_DemonstratesExceptionApi()
    {
        // MSTest v4: Assert.ThrowsExactly<T> (replaces obsolete ThrowsException / ExpectedException)
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            throw new ArgumentNullException("ownerIdNumber"));
        Assert.IsNotNull(ex);
        Assert.AreEqual("ownerIdNumber", ex.ParamName);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_InvalidBranchCode_ReturnsFailed()
    {
        // Arrange
        _mockValidator.Setup(v => v.IsValidBranchCode("INVALID_BRANCH")).Returns(false);

        // Act
        var result = _accountService.CreateAccount("John Doe", "1234567890123", 
            AccountType.Savings, 500m, "INVALID_BRANCH");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        StringAssert.Contains(result.Message, "branch");
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void CreateAccount_MockVerification_RepositoryNotCalledForInvalidInput()
    {
        // Arrange
        _mockValidator.Setup(v => v.IsValidName(It.IsAny<string>())).Returns(false);

        // Act
        var result = _accountService.CreateAccount("", "1234567890123", AccountType.Savings, 500m, "BRANCH001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        // Verify Add was NOT called because validation failed
        _mockAccountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
    }
}
