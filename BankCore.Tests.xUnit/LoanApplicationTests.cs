using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>TC-LOAN-001, 002, 009, 012, 015–020</summary>
public class LoanApplicationTests
{
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public LoanApplicationTests()
    {
        var account = new Account { Id = 1, Status = AccountStatus.Active, Balance = 1000m };
        _accountRepo.Setup(r => r.GetById(1)).Returns(account);
        _loanRepo.Setup(r => r.Add(It.IsAny<Loan>()));
        _svc = new LoanService(_loanRepo.Object, _accountRepo.Object, _audit.Object);
    }

    /// <summary>TC-LOAN-001</summary>
    [Fact]
    public void ApplyForLoan_ValidApplication_Succeeds()
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 50_000m, 36, 0.12m, 20_000m, 2_000m, 700);
        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Status.Should().Be(LoanStatus.Pending);
        _loanRepo.Verify(r => r.Add(It.IsAny<Loan>()), Times.Once);
        _loanRepo.Verify(r => r.Add(It.Is<Loan>(l => l.PrincipalAmount == 50_000m)), Times.Exactly(1));
    }

    /// <summary>TC-LOAN-002</summary>
    [Fact]
    public void ApproveLoan_EligiblePending_Succeeds()
    {
        var loan = new Loan
        {
            LoanReference = "LN-TEST-001",
            AccountId = 1,
            Status = LoanStatus.Pending,
            PrincipalAmount = 50_000m,
            OutstandingBalance = 50_000m,
            InterestRate = 0.12m,
            TermMonths = 36
        };
        _loanRepo.Setup(r => r.GetByReference("LN-TEST-001")).Returns(loan);
        _loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        var result = _svc.ApproveLoan("LN-TEST-001", "manager1");
        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Status.Should().Be(LoanStatus.Active);
    }

    /// <summary>TC-LOAN-009</summary>
    [Fact]
    public void ApplyForLoan_LowCreditScore_Fails()
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 10_000m, 12, 0.10m, 15_000m, 0m, 500);
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("credit score");
    }

    /// <summary>TC-LOAN-012</summary>
    [Fact]
    public void ApplyForLoan_NegativeAmount_Fails()
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, -100m, 12, 0.10m, 15_000m, 0m, 700);
        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>TC-LOAN-015 / 016</summary>
    [Theory]
    [InlineData(0, false)]
    [InlineData(0.01, true)]
    [InlineData(1000, true)]
    public void ApplyForLoan_AmountBoundaries(decimal amount, bool expectSuccess)
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, amount, 12, 0.10m, 30_000m, 0m, 700);
        result.IsSuccess.Should().Be(expectSuccess, result.Message);
    }

    /// <summary>TC-LOAN-017 / 018 — rate and term policy boundaries</summary>
    [Theory]
    [InlineData(3, true)]
    [InlineData(360, true)]
    [InlineData(2, false)]
    [InlineData(361, false)]
    public void ApplyForLoan_TermBoundaries(int term, bool expectSuccess)
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 10_000m, term, 0.10m, 30_000m, 0m, 700);
        result.IsSuccess.Should().Be(expectSuccess, result.Message);
    }

    /// <summary>TC-LOAN-019 / 020</summary>
    [Theory]
    [InlineData(0.01, true)]
    [InlineData(0.40, true)]
    [InlineData(0.41, false)]
    [InlineData(0, false)]
    public void ApplyForLoan_RateBoundaries(double rate, bool expectSuccess)
    {
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 10_000m, 12, (decimal)rate, 30_000m, 0m, 700);
        result.IsSuccess.Should().Be(expectSuccess, result.Message);
    }
}
