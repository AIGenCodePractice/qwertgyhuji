using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankCore.Tests.MSTest;

[TestClass]
public class ValidationBranchCoverageTests
{
    private readonly ValidationService _validation = new();

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("123")]
    [DataRow("0000000000000")]
    [DataRow("9901325009087")]
    public void SouthAfricanId_InvalidInputs_ReturnFalse(string? id)
    {
        Assert.IsFalse(_validation.IsValidSouthAfricanIdNumber(id!));
    }

    [TestMethod]
    public void SouthAfricanId_ValidAndSpacedValues_CoverLuhnBranches()
    {
        Assert.IsTrue(_validation.IsValidSouthAfricanIdNumber("8001015009087"));
        Assert.IsTrue(_validation.IsValidSouthAfricanIdNumber("800101 5009 087"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("BC123")]
    [DataRow("AC1234567890")]
    [DataRow("BC12345678901")]
    public void AccountNumber_InvalidFormats_ReturnFalse(string? value)
    {
        Assert.IsFalse(_validation.IsValidAccountNumber(value!));
    }

    [TestMethod]
    [DataRow(0.00)]
    [DataRow(0.009)]
    [DataRow(0.01)]
    [DataRow(100.00)]
    [DataRow(999999.99)]
    [DataRow(1000000.00)]
    public void Amount_CoversInclusiveBoundaries(decimal amount)
    {
        var expected = amount >= 0.01m && amount <= 999999.99m;
        Assert.AreEqual(expected, _validation.IsValidAmount(amount));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("A")]
    [DataRow("ThisNameContains123")]
    [DataRow("Jane Doe")]
    [DataRow("O'Connor-Smith")]
    public void Name_CoversWhitespaceLengthAndRegex(string? name)
    {
        var result = _validation.IsValidName(name!);
        if (name is "Jane Doe" or "O'Connor-Smith") Assert.IsTrue(result); else Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("abc")]
    [DataRow("abcdefghijklmnopqrstu")]
    [DataRow("valid.user_1")]
    [DataRow("bad-user")]
    public void Username_CoversLengthAndRegex(string? username)
    {
        var result = _validation.IsValidUsername(username!);
        if (username == "valid.user_1") Assert.IsTrue(result); else Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("short")]
    [DataRow("alllowercase1!")]
    [DataRow("ALLUPPERCASE1!")]
    [DataRow("NoDigitsHere!")]
    [DataRow("NoSpecial123")]
    [DataRow("GoodPass1!")]
    public void Password_CoversAllComplexityBranches(string? password)
    {
        var result = _validation.IsValidPassword(password!);
        Assert.AreEqual(password == "GoodPass1!", result);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("12345")]
    [DataRow("123456")]
    [DataRow("ABC123")]
    public void BranchCode_CoversValidAndInvalidFormats(string? branch)
    {
        Assert.AreEqual(branch == "123456", _validation.IsValidBranchCode(branch!));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("missing-at.example.com")]
    [DataRow("user@localhost")]
    [DataRow("user@example.com")]
    public void Email_CoversRegexBranches(string? email)
    {
        Assert.AreEqual(email == "user@example.com", _validation.IsValidEmail(email!));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("Normal input")]
    [DataRow("SELECT * FROM users")]
    [DataRow("hello <script>")]
    [DataRow("name -- comment")]
    public void SafeInput_CoversEmptyKeywordsAndMarkup(string input)
    {
        var expected = input is "" or "Normal input";
        Assert.AreEqual(expected, _validation.IsSafeInput(input));
    }
}
