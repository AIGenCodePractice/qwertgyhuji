using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>TC-LOAN-006, 007</summary>
public class EarlySettlementTests
{
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public EarlySettlementTests()
    {
        _svc = new LoanService(_loanRepo.Object, _accountRepo.Object, _audit.Object);
    }

    /// <summary>TC-LOAN-006</summary>
    [Fact]
    public void CalculateSettlementAmount_IncludesFee()
    {
        var loan = new Loan
        {
            LoanReference = "LN-SET",
            Status = LoanStatus.Active,
            PrincipalAmount = 100_000m,
            OutstandingBalance = 50_000m
        };
        _loanRepo.Setup(r => r.GetByReference("LN-SET")).Returns(loan);

        var result = _svc.CalculateSettlementAmount("LN-SET");
        result.IsSuccess.Should().BeTrue(result.Message);
        // BUG-012: fee on principal (1.5% of 100000) + outstanding 50000
        result.Data.Should().Be(50_000m + 100_000m * 0.015m);
    }

    /// <summary>TC-LOAN-007</summary>
    [Fact]
    public void SettleLoan_ClosesLoan()
    {
        var loan = new Loan
        {
            LoanReference = "LN-SET2",
            Status = LoanStatus.Active,
            PrincipalAmount = 20_000m,
            OutstandingBalance = 10_000m
        };
        _loanRepo.Setup(r => r.GetByReference("LN-SET2")).Returns(loan);
        _loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));

        var result = _svc.SettleLoan("LN-SET2", "teller1");
        result.IsSuccess.Should().BeTrue(result.Message);
        loan.Status.Should().Be(LoanStatus.Settled);
        loan.OutstandingBalance.Should().Be(0m);
    }
}
