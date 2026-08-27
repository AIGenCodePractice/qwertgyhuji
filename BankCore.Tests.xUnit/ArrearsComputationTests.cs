using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>TC-LOAN-021, 022</summary>
public class ArrearsComputationTests
{
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public ArrearsComputationTests()
    {
        _svc = new LoanService(_loanRepo.Object, _accountRepo.Object, _audit.Object);
    }

    /// <summary>TC-LOAN-021 — loan in Arrears state with penalty amount tracked</summary>
    [Fact]
    public void Loan_InArrears_TracksPenaltyFields()
    {
        var loan = new Loan
        {
            LoanReference = "LN-ARR",
            Status = LoanStatus.Arrears,
            OutstandingBalance = 8_000m,
            MonthlyInstalment = 500m,
            MissedPayments = 2,
            ArrearsAmount = 500m * 2 + (500m * 0.02m * 2) // instalments + penalty model
        };
        _loanRepo.Setup(r => r.GetByReference("LN-ARR")).Returns(loan);

        var result = _svc.GetLoan("LN-ARR");
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(LoanStatus.Arrears);
        result.Data.MissedPayments.Should().BeGreaterThan(0);
        result.Data.ArrearsAmount.Should().BeGreaterThan(0);
    }

    /// <summary>TC-LOAN-022 — further disbursement blocked while Arrears (approve only Pending)</summary>
    [Fact]
    public void ApproveLoan_WhenAlreadyArrears_Fails()
    {
        var loan = new Loan { LoanReference = "LN-ARR2", Status = LoanStatus.Arrears };
        _loanRepo.Setup(r => r.GetByReference("LN-ARR2")).Returns(loan);

        var result = _svc.ApproveLoan("LN-ARR2", "mgr");
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Pending");
    }
}
