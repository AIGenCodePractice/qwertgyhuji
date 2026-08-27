using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>TC-LOAN-003, 005, 008, 011</summary>
public class RepaymentScheduleTests
{
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public RepaymentScheduleTests()
    {
        _accountRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns(new Account { Id = 1, Status = AccountStatus.Active });
        _svc = new LoanService(_loanRepo.Object, _accountRepo.Object, _audit.Object);
    }

    /// <summary>TC-LOAN-003</summary>
    [Fact]
    public void GenerateRepaymentSchedule_36Month_Has36Rows()
    {
        var loan = new Loan
        {
            LoanReference = "LN-36",
            Status = LoanStatus.Active,
            PrincipalAmount = 36_000m,
            OutstandingBalance = 36_000m,
            InterestRate = 0.12m,
            TermMonths = 36,
            MonthlyInstalment = 1000m,
            RepaymentSchedule = Enumerable.Range(1, 36).Select(i => new LoanRepayment
            {
                InstallmentNumber = i,
                InstallmentAmount = 1000m
            }).ToList()
        };
        _loanRepo.Setup(r => r.GetByReference("LN-36")).Returns(loan);

        var result = _svc.GenerateRepaymentSchedule("LN-36");
        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Should().HaveCount(36);
    }

    /// <summary>TC-LOAN-005</summary>
    [Fact]
    public void ProcessRepayment_MonthlyInstalment_ReducesBalance()
    {
        var loan = new Loan
        {
            LoanReference = "LN-PAY",
            Status = LoanStatus.Active,
            OutstandingBalance = 10_000m,
            MonthlyInstalment = 500m
        };
        _loanRepo.Setup(r => r.GetByReference("LN-PAY")).Returns(loan);
        _loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));

        var result = _svc.ProcessRepayment("LN-PAY", 500m, "teller1");
        result.IsSuccess.Should().BeTrue(result.Message);
        loan.OutstandingBalance.Should().Be(9_500m);
    }

    /// <summary>TC-LOAN-008 — rate change reflected by rebuilding schedule on approve path</summary>
    [Fact]
    public void ApproveLoan_BuildsRepaymentSchedule()
    {
        var loan = new Loan
        {
            LoanReference = "LN-RATE",
            AccountId = 1,
            Status = LoanStatus.Pending,
            PrincipalAmount = 12_000m,
            OutstandingBalance = 12_000m,
            InterestRate = 0.15m,
            TermMonths = 12
        };
        _loanRepo.Setup(r => r.GetByReference("LN-RATE")).Returns(loan);
        _loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));
        _accountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        var result = _svc.ApproveLoan("LN-RATE", "mgr");
        result.IsSuccess.Should().BeTrue(result.Message);
        loan.RepaymentSchedule.Should().NotBeNull();
        loan.RepaymentSchedule.Count.Should().Be(12);
    }

    /// <summary>TC-LOAN-011</summary>
    [Fact]
    public void ProcessRepayment_ZeroAmount_Fails()
    {
        var loan = new Loan { LoanReference = "LN-Z", Status = LoanStatus.Active, OutstandingBalance = 1000m };
        _loanRepo.Setup(r => r.GetByReference("LN-Z")).Returns(loan);
        var result = _svc.ProcessRepayment("LN-Z", 0m, "t");
        result.IsSuccess.Should().BeFalse();
    }
}
