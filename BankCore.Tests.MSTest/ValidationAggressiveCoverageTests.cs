using BankCore.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankCore.Tests.MSTest;

[TestClass]
public class ValidationAggressiveCoverageTests
{
    private readonly ValidationService _validation = new();

    [DataTestMethod]
    [DataRow("8001015009087", true)]
    [DataRow("8001015009088", false)]
    [DataRow("9913995009087", false)]
    [DataRow("0000005009087", false)]
    [DataRow("8001005009087", false)]
    [DataRow("8001015009089", false)]
    [DataRow("800101 5009 087", true)]
    [DataRow("800101x5009087", false)]
    [DataRow("123456789012", false)]
    public void SouthAfricanId_AggressivePartitions(string id, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidSouthAfricanIdNumber(id));

    [DataTestMethod]
    [DataRow("BC0000000000", true)]
    [DataRow("BC9999999999", true)]
    [DataRow("bc1234567890", false)]
    [DataRow("BC123456789", false)]
    [DataRow("BC12345678901", false)]
    [DataRow("BC123456789A", false)]
    [DataRow("XX1234567890", false)]
    [DataRow(" BC1234567890", false)]
    public void AccountNumber_AggressivePartitions(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidAccountNumber(value));

    [TestMethod]
    public void Amount_CustomBoundaries_AreExercised()
    {
        Assert.IsFalse(_validation.IsValidAmount(10m, 20m, 30m));
        Assert.IsTrue(_validation.IsValidAmount(20m, 20m, 30m));
        Assert.IsTrue(_validation.IsValidAmount(30m, 20m, 30m));
        Assert.IsFalse(_validation.IsValidAmount(31m, 20m, 30m));
        Assert.IsTrue(_validation.IsValidAmount(0.01m));
        Assert.IsTrue(_validation.IsValidAmount(999999.99m));
    }

    [DataTestMethod]
    [DataRow("Jo", true)]
    [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", true)]
    [DataRow("A", false)]
    [DataRow("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", false)]
    [DataRow("Jane123", false)]
    [DataRow("Jane@Doe", false)]
    [DataRow("Jane.Doe", true)]
    [DataRow("Jane-Doe", true)]
    [DataRow("Jane Doe", true)]
    public void Name_AggressivePartitions(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidName(value));

    [TestMethod]
    [DataRow("abcd", true)]
    [DataRow("aaaaaaaaaaaaaaaaaaaa", true)]
    [DataRow("abc", false)]
    [DataRow("aaaaaaaaaaaaaaaaaaaaa", false)]
    [DataRow("user name", false)]
    [DataRow("user-name", false)]
    [DataRow("user.name", true)]
    [DataRow("user_1", true)]
    [DataRow("user.1", true)]
    public void Username_AggressivePartitions(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidUsername(value));

    [TestMethod]
    [DataRow("Abcdef1!", true)]
    [DataRow("abcdef1!", false)]
    [DataRow("ABCDEF1!", false)]
    [DataRow("Abcdefgh", false)]
    [DataRow("Abcdefg1", false)]
    [DataRow("Abcdefg!", false)]
    [DataRow("Abc1!", false)]
    [DataRow("Abcdef1@", true)]
    [DataRow("Abcdef1<", true)]
    public void Password_EachComplexityCondition(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidPassword(value));

    [TestMethod]
    [DataRow("000000", true)]
    [DataRow("999999", true)]
    [DataRow("12345", false)]
    [DataRow("1234567", false)]
    [DataRow("12345A", false)]
    [DataRow(" 123456", false)]
    public void BranchCode_BoundaryAndCharacterPartitions(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidBranchCode(value));

    [TestMethod]
    [DataRow("a@b.c", true)]
    [DataRow("user.name@example.co.za", true)]
    [DataRow("user@localhost", false)]
    [DataRow("userexample.com", false)]
    [DataRow("@example.com", false)]
    [DataRow("user@.com", false)]
    [DataRow("user @example.com", false)]
    public void Email_AggressiveRegexPartitions(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsValidEmail(value));

    [TestMethod]
    [DataRow("", true)]
    [DataRow("SELECT", false)]
    [DataRow("insert into", false)]
    [DataRow("DELETE something", false)]
    [DataRow("drop table", false)]
    [DataRow("create user", false)]
    [DataRow("ALTER USER", false)]
    [DataRow("EXEC xp_cmdshell", false)]
    [DataRow("UNION SELECT", false)]
    [DataRow("truncate table", false)]
    [DataRow("hello -- comment", false)]
    [DataRow("hello /* comment", false)]
    [DataRow("hello */", false)]
    [DataRow("<script>", false)]
    [DataRow("a > b", false)]
    [DataRow("safe input & text", true)]
    [DataRow("special ; punctuation", true)]
    public void SafeInput_AllSecurityPatternFamilies(string value, bool expected)
        => Assert.AreEqual(expected, _validation.IsSafeInput(value));
}
