using BankCore.Core.Interfaces;
using System.Text.RegularExpressions;

namespace BankCore.Core.Services;

/// <summary>
/// Provides input validation for all system entities.
/// </summary>
public class ValidationService : IValidationService
{
    private static readonly HashSet<string> _sqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER",
        "EXEC", "EXECUTE", "UNION", "TRUNCATE", "--", "/*", "*/"
    };

    /// <summary>
    /// Validates a South African ID number using the Luhn algorithm.
    /// Format: YYMMDD SSSS C A Z (13 digits).
    /// </summary>
    public bool IsValidSouthAfricanIdNumber(string idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber)) return false;

        idNumber = idNumber.Replace(" ", "");
        if (idNumber.Length != 13 || !idNumber.All(char.IsDigit)) return false;

        int month = int.Parse(idNumber.Substring(2, 2));
        int day = int.Parse(idNumber.Substring(4, 2));

        if (month < 1 || month > 12) return false;
        if (day < 1 || day > 31) return false;

        int total = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = idNumber[i] - '0';
            if (i % 2 == 0)
                total += digit;
            else
            {
                int doubled = digit * 2;
                total += doubled > 9 ? doubled - 9 : doubled;
            }
        }

        int checkDigit = (10 - (total % 10)) % 10;
        return checkDigit == idNumber[12] - '0';
    }

    /// <summary>
    /// Account number must be 10 digits starting with "BC".
    /// </summary>
    public bool IsValidAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return false;
        return Regex.IsMatch(accountNumber, @"^BC\d{10}$");
    }

    /// <summary>
    /// Amount must be between min and max inclusive.
    /// </summary>
    public bool IsValidAmount(decimal amount, decimal min = 0.01m, decimal max = 999999.99m)
        => amount >= min && amount <= max;

    public bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length < 2 || name.Length > 100) return false;
        return Regex.IsMatch(name, @"^[a-zA-Z\s\-'\.]+$");
    }

    public bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;
        if (username.Length < 4 || username.Length > 20) return false;
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_\.]+$");
    }

    /// <summary>
    /// Password must be at least 8 characters with uppercase, lowercase, digit, and special char.
    /// </summary>
    public bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 8) return false;
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    public bool IsValidBranchCode(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode)) return false;
        return Regex.IsMatch(branchCode, @"^\d{6}$");
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    /// <summary>
    /// Checks for SQL injection and XSS patterns.
    /// </summary>
    public bool IsSafeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return true;
        foreach (var keyword in _sqlKeywords)
        {
            if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return false;
        }
        if (input.Contains('<') || input.Contains('>')) return false;
        return true;
    }
}
