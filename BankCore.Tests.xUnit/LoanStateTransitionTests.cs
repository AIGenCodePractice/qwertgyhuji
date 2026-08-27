using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>TC-LOAN-004, 010, 013, 014</summary>
public class LoanStateTransitionTests
{
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public LoanStateTransitionTests()
    {
        _accountRepo.Setup(r => r.GetById(1)).Returns(new Account { Id = 1, Status = AccountStatus.Active, Balance = 0m });
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));
        _loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));
        _svc = new LoanService(_loanRepo.Object, _accountRepo.Object, _audit.Object);
    }

    /// <summary>TC-LOAN-004 — Approve (Pending → Active) credits linked account</summary>
    [Fact]
    public void Approve_DisbursesToLinkedAccount()
    {
        var loan = new Loan
        {
            LoanReference = "LN-DIS",
            AccountId = 1,
            Status = LoanStatus.Pending,
            PrincipalAmount = 25_000m,
            OutstandingBalance = 25_000m,
            InterestRate = 0.1m,
            TermMonths = 24
        };
        _loanRepo.Setup(r => r.GetByReference("LN-DIS")).Returns(loan);
        var account = new Account { Id = 1, Status = AccountStatus.Active, Balance = 100m };
        _accountRepo.Setup(r => r.GetById(1)).Returns(account);

        var result = _svc.ApproveLoan("LN-DIS", "mgr");
        result.IsSuccess.Should().BeTrue(result.Message);
        account.Balance.Should().Be(25_100m);
        loan.Status.Should().Be(LoanStatus.Active);
    }

    /// <summary>TC-LOAN-010</summary>
    [Fact]
    public void Approve_StillPendingRequired_RejectsActive()
    {
        var loan = new Loan { LoanReference = "LN-ACT", Status = LoanStatus.Active };
        _loanRepo.Setup(r => r.GetByReference("LN-ACT")).Returns(loan);
        var result = _svc.ApproveLoan("LN-ACT", "mgr");
        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>TC-LOAN-013 — cannot go Settled → Active via Approve</summary>
    [Fact]
    public void Approve_FromSettled_Fails()
    {
        var loan = new Loan { LoanReference = "LN-SET", Status = LoanStatus.Settled };
        _loanRepo.Setup(r => r.GetByReference("LN-SET")).Returns(loan);
        var result = _svc.ApproveLoan("LN-SET", "mgr");
        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>TC-LOAN-014 — cannot Approve from Active back to Pending (no API); Reject requires Pending</summary>
    [Fact]
    public void Reject_FromActive_Fails()
    {
        var loan = new Loan { LoanReference = "LN-ACT2", Status = LoanStatus.Active };
        _loanRepo.Setup(r => r.GetByReference("LN-ACT2")).Returns(loan);
        var result = _svc.RejectLoan("LN-ACT2", "nope", "mgr");
        result.IsSuccess.Should().BeFalse();
    }
}
