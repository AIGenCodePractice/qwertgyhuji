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
        result.Data.Should().Be(50_000m + 50_000m * 0.015m);
        _loanRepo.Verify(r => r.GetByReference("LN-SET"), Times.Once);
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
        _loanRepo.Verify(r => r.GetByReference("LN-SET2"), Times.Exactly(2));
        _loanRepo.Verify(r => r.Update(It.Is<Loan>(l =>
            l.LoanReference == "LN-SET2" &&
            l.Status == LoanStatus.Settled &&
            l.OutstandingBalance == 0m)), Times.Once);
        _audit.Verify(a => a.Log(
            "LOAN_SETTLED",
            "teller1",
            It.Is<string>(message => message.Contains("LN-SET2")),
            "LN-SET2",
            true,
            It.IsAny<string>()), Times.Once);
    }
}
