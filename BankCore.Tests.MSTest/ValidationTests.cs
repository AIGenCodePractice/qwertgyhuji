using BankCore.Core.Services;

namespace BankCore.Tests.MSTest;

/// <summary>
/// TC-VAL-001 through TC-VAL-020
/// Verify the shared input validation library accepts valid input and rejects invalid input.
/// Uses the real ValidationService (not a mock) so production rules are exercised.
/// </summary>
[TestClass]
public class ValidationTests
{
    private ValidationService _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new ValidationService();
    }

    [TestCleanup]
    public void Teardown()
    {
        _validator = null!;
    }

    // ─── Positive ───────────────────────────────────────────────────────────

    /// <summary>TC-VAL-001 / TC-VAL-020 — Accept valid South African ID number format and checksum</summary>
    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("9001015800088")]
    [DataRow("8001015009087")]
    public void IsValidSouthAfricanIdNumber_Valid13Digit_ReturnsTrue(string id)
    {
        Assert.IsTrue(_validator.IsValidSouthAfricanIdNumber(id));
    }

    /// <summary>TC-VAL-002 — Accept valid email address</summary>
    [TestMethod]
    [TestCategory("Functional")]
    [DataRow("user@example.com")]
    [DataRow("thabo.molefe@bank.co.za")]
    [DataRow("a@b.c")]
    public void IsValidEmail_Valid_ReturnsTrue(string email)
    {
        Assert.IsTrue(_validator.IsValidEmail(email));
    }

    /// <summary>TC-VAL-003 — Accept valid positive money amount</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidAmount_PositiveInRange_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidAmount(100.50m));
        Assert.IsTrue(_validator.IsValidAmount(0.01m));
        Assert.IsTrue(_validator.IsValidAmount(5000m));
    }

    /// <summary>TC-VAL-004 — Accept valid South African phone-style numeric input via name/safe checks</summary>
    [TestMethod]
    [TestCategory("Functional")]
    public void IsSafeInput_PlainPhoneDigits_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsSafeInput("0821234567"));
    }

    /// <summary>TC-VAL-015 — Accept amount of exactly R0.00 when min allows</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_ExactlyZero_WithMinZero_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidAmount(0.00m, min: 0m, max: 1000m));
    }

    /// <summary>TC-VAL-016 — Accept amount of R0.01</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_OneCent_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidAmount(0.01m));
    }

    /// <summary>TC-VAL-017 — Accept string of exactly maximum length (name max 100)</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidName_ExactMaxLength_ReturnsTrue()
    {
        var name = new string('A', 100);
        Assert.IsTrue(_validator.IsValidName(name));
    }

    // ─── Negative ───────────────────────────────────────────────────────────

    /// <summary>TC-VAL-005 — Reject null string for a required field</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_Null_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidName(null!));
    }

    /// <summary>TC-VAL-006 — Reject empty string for a required field</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_Empty_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidName(""));
        Assert.IsFalse(_validator.IsValidName("   "));
    }

    /// <summary>TC-VAL-007 — Reject negative amount</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidAmount_Negative_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidAmount(-0.01m));
        Assert.IsFalse(_validator.IsValidAmount(-100m));
    }

    /// <summary>TC-VAL-008 — Reject invalid email format</summary>
    [TestMethod]
    [TestCategory("Negative")]
    [DataRow("notanemail")]
    [DataRow("@nodomain.com")]
    [DataRow("user@")]
    [DataRow("")]
    public void IsValidEmail_Invalid_ReturnsFalse(string email)
    {
        Assert.IsFalse(_validator.IsValidEmail(email));
    }

    /// <summary>TC-VAL-009 — Reject or sanitise SQL-injection style input</summary>
    [TestMethod]
    [TestCategory("Negative")]
    [TestCategory("Security")]
    [DataRow("'; DROP TABLE users; --")]
    [DataRow("1; UNION SELECT * FROM users")]
    [DataRow("SELECT * FROM accounts")]
    [DataRow("<script>alert(1)</script>")]
    public void IsSafeInput_SqlInjectionOrXss_ReturnsFalse(string input)
    {
        Assert.IsFalse(_validator.IsSafeInput(input),
            $"Expected unsafe for: {input}");
    }

    /// <summary>
    /// Documents production gap: classic boolean-OR injection without SQL keywords
    /// is treated as safe because IsSafeInput only scans a fixed keyword list
    /// (SELECT/UNION/DROP/--) and angle brackets — not standalone OR.
    /// </summary>
    [TestMethod]
    [TestCategory("Negative")]
    [TestCategory("Security")]
    public void IsSafeInput_ClassicOrInjection_WithoutKeywords_CurrentlyAllowed_DocumentsGap()
    {
        const string payload = "1' OR '1'='1";
        Assert.IsTrue(_validator.IsSafeInput(payload),
            "Known gap: payload has no listed SQL keyword; production returns true.");
    }

    /// <summary>TC-VAL-010 — Reject amount above the default inclusive maximum</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    public void IsValidAmount_AboveMax_ReturnsFalse()
    {
        Assert.IsTrue(_validator.IsValidAmount(999_999.99m));
        Assert.IsFalse(_validator.IsValidAmount(1_000_000.00m));
    }

    /// <summary>TC-VAL-011 — Reject phone/name containing invalid characters</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidName_ContainsDigits_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidName("John123"));
    }

    /// <summary>TC-VAL-012 — Reject ID number with invalid length</summary>
    [TestMethod]
    [TestCategory("Negative")]
    [DataRow("123")]
    [DataRow("123456789012")]   // 12 digits
    [DataRow("12345678901234")] // 14 digits
    [DataRow("ABCDEFGHIJKLM")]
    public void IsValidSouthAfricanIdNumber_InvalidLengthOrAlpha_ReturnsFalse(string id)
    {
        Assert.IsFalse(_validator.IsValidSouthAfricanIdNumber(id));
    }

    /// <summary>TC-VAL-013 / TC-VAL-018 — Reject string longer than maximum allowed length</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    [TestCategory("Negative")]
    public void IsValidName_MaxPlusOne_ReturnsFalse()
    {
        var name = new string('A', 101);
        Assert.IsFalse(_validator.IsValidName(name));
    }

    /// <summary>TC-VAL-014 — Reject special characters in a numeric-only field (account number)</summary>
    [TestMethod]
    [TestCategory("Negative")]
    public void IsValidAccountNumber_SpecialChars_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidAccountNumber("BC!@#$%^&*()"));
        Assert.IsFalse(_validator.IsValidAccountNumber("1234567890"));
    }

    /// <summary>TC-VAL-019 — Reject amount just above maximum</summary>
    [TestMethod]
    [TestCategory("Boundary")]
    [TestCategory("Negative")]
    public void IsValidAmount_JustAboveMax_ReturnsFalse()
    {
        Assert.IsFalse(_validator.IsValidAmount(1_000_000.00m, min: 0.01m, max: 999_999.99m));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidAccountNumber_ValidFormat_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidAccountNumber("BC1000000001"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void IsValidBranchCode_SixDigits_ReturnsTrue()
    {
        Assert.IsTrue(_validator.IsValidBranchCode("250655"));
    }

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
    [TestCategory("Functional")]
    public void IsValidName_NullDoesNotThrow_ReturnsFalse()
    {
        // Validation methods must not throw on null — return false
        Assert.IsFalse(_validator.IsValidName(null!));
    }
}
