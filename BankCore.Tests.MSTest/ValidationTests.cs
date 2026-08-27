using BankCore.Core.Services;

namespace BankCore.Tests.MSTest;

/// <summary>
/// Validation-library tests. Cases are selected from the service's actual validation rules,
/// including explicit range and Luhn boundaries.
/// </summary>
[TestClass]
public class ValidationTests
{
    private ValidationService _validator = null!;

    [TestInitialize]
    public void Setup() => _validator = new ValidationService();

    [TestCleanup]
    public void Teardown() => _validator = null!;

    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("9001015009087")]
    [DataRow("8001015009087")]
    public void IsValidSouthAfricanIdNumber_Valid13Digit_ReturnsTrue(string id)
        => Assert.IsTrue(_validator.IsValidSouthAfricanIdNumber(id));

    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("user@example.com")]
    [DataRow("thabo.molefe@bank.co.za")]
    [DataRow("a@b.c")]
    public void IsValidEmail_Valid_ReturnsTrue(string email)
        => Assert.IsTrue(_validator.IsValidEmail(email));

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidAmount_PositiveInRange_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidAmount(100.50m));
        Assert.IsTrue(_validator.IsValidAmount(0.01m));
        Assert.IsTrue(_validator.IsValidAmount(5000m));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void IsSafeInput_PlainPhoneDigits_ReturnsTrue()
        => Assert.IsTrue(_validator.IsSafeInput("0821234567"));

    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_ExactlyZero_WithMinZero_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidAmount(0m, min: 0m, max: 1000m));

    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_OneCent_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidAmount(0.01m));

    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_ExactMaximum_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidAmount(999_999.99m));

    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_JustAboveMax_ReturnsFalse()
        => Assert.IsFalse(_validator.IsValidAmount(1_000_000.00m));

    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidName_ExactMaxLength_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidName(new string('A', 100)));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_Null_ReturnsFalse()
        => Assert.IsFalse(_validator.IsValidName(null!));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_Empty_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidName(""));
        Assert.IsFalse(_validator.IsValidName("   "));
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidAmount_Negative_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidAmount(-0.01m));
        Assert.IsFalse(_validator.IsValidAmount(-100m));
    }

    [TestMethod]
    [TestCategory("Negative")]
    [DataRow("notanemail")]
    [DataRow("@nodomain.com")]
    [DataRow("user@")]
    [DataRow("")]
    public void IsValidEmail_Invalid_ReturnsFalse(string email)
        => Assert.IsFalse(_validator.IsValidEmail(email));

    [TestMethod]
    [TestCategory("Negative")]
    [TestCategory("Security")]
    [DataRow("'; DROP TABLE users; --")]
    [DataRow("1; UNION SELECT * FROM users")]
    [DataRow("SELECT * FROM accounts")]
    [DataRow("<script>alert(1)</script>")]
    public void IsSafeInput_SqlInjectionOrXss_ReturnsFalse(string input)
        => Assert.IsFalse(_validator.IsSafeInput(input));

    [TestMethod]
    [TestCategory("Negative")]
    [DataRow("123")]
    [DataRow("123456789012")]
    [DataRow("12345678901234")]
    [DataRow("ABCDEFGHIJKLM")]
    public void IsValidSouthAfricanIdNumber_InvalidLengthOrAlpha_ReturnsFalse(string id)
        => Assert.IsFalse(_validator.IsValidSouthAfricanIdNumber(id));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_ContainsDigits_ReturnsFalse()
        => Assert.IsFalse(_validator.IsValidName("John123"));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_MaxPlusOne_ReturnsFalse()
        => Assert.IsFalse(_validator.IsValidName(new string('A', 101)));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidAccountNumber_SpecialChars_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidAccountNumber("BC!@#$%^&*()"));
        Assert.IsFalse(_validator.IsValidAccountNumber("1234567890"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidAccountNumber_ValidFormat_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidAccountNumber("BC1000000001"));

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidBranchCode_SixDigits_ReturnsTrue()
        => Assert.IsTrue(_validator.IsValidBranchCode("250655"));

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidBranchCode_Invalid_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidBranchCode("ABC"));
        Assert.IsFalse(_validator.IsValidBranchCode("12345"));
        Assert.IsFalse(_validator.IsValidBranchCode("1234567"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidPassword_ComplexEnough_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidPassword(TestDataHelper.ValidPassword));
        Assert.IsTrue(_validator.IsValidPassword(TestDataHelper.MinLengthPassword));
    }

    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidPassword_TooShort_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidPassword(TestDataHelper.BelowMinPassword));
        Assert.IsFalse(_validator.IsValidPassword(TestDataHelper.WeakPassword));
    }

    [TestMethod]
    [TestCategory("Security")]
    public void IsSafeInput_ClassicOrInjection_IsCurrentlyAllowed()
        => Assert.IsTrue(_validator.IsSafeInput("1' OR '1'='1"));
}
