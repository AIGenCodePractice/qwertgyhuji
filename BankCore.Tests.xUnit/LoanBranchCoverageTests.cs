using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankCore.Tests.xUnit;

public class LoanBranchCoverageTests
{
    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly LoanService _svc;

    public LoanBranchCoverageTests()
    {
        _accounts.Setup(r => r.Update(It.IsAny<Account>()));
        _loans.Setup(r => r.Update(It.IsAny<Loan>()));
        _svc = new LoanService(_loans.Object, _accounts.Object, _audit.Object);
    }

    private static Account ActiveAccount() => new() { Id = 1, Status = AccountStatus.Active, Balance = 0m };

    [Theory]
    [InlineData(0, 12, 0.1, 10000, 0, 700, "positive")]
    [InlineData(1000, 2, 0.1, 10000, 0, 700, "term")]
    [InlineData(1000, 361, 0.1, 10000, 0, 700, "term")]
    [InlineData(1000, 12, 0, 10000, 0, 700, "Interest")]
    [InlineData(1000, 12, 0.41, 10000, 0, 700, "Interest")]
    [InlineData(1000, 12, 0.1, 0, 0, 700, "income")]
    [InlineData(1000, 12, 0.1, 10000, -1, 700, "debt")]
    [InlineData(1000, 12, 0.1, 10000, 0, 599, "credit")]
    public void ApplyForLoan_ValidationBranches_ReturnFailure(decimal amount, int term, decimal rate, decimal income, decimal debt, int score, string fragment)
    {
        _accounts.Setup(r => r.GetById(1)).Returns(ActiveAccount());
        var result = _svc.ApplyForLoan(1, LoanType.Personal, amount, term, rate, income, debt, score);
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().ContainEquivalentOf(fragment);
    }

    [Fact]
    public void ApplyForLoan_MissingAndInactiveAccountBranches_ReturnFailure()
    {
        _accounts.Setup(r => r.GetById(1)).Returns((Account?)null);
        _svc.ApplyForLoan(1, LoanType.Personal, 1000m, 12, 0.1m, 10000m, 0m, 700).IsSuccess.Should().BeFalse();

        _accounts.Setup(r => r.GetById(1)).Returns(new Account { Id = 1, Status = AccountStatus.Closed });
        _svc.ApplyForLoan(1, LoanType.Personal, 1000m, 12, 0.1m, 10000m, 0m, 700).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ApplyForLoan_HighDebtToIncomeBranch_ReturnsFailure()
    {
        _accounts.Setup(r => r.GetById(1)).Returns(ActiveAccount());
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 10000m, 12, 0.2m, 1000m, 500m, 700);
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Debt-to-income");
    }

    [Fact]
    public void ApplyForLoan_ValidBranch_AddsPendingLoan()
    {
        _accounts.Setup(r => r.GetById(1)).Returns(ActiveAccount());
        var result = _svc.ApplyForLoan(1, LoanType.Personal, 1000m, 12, 0.1m, 10000m, 0m, 700);
        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(LoanStatus.Pending);
        _loans.Verify(r => r.Add(It.IsAny<Loan>()), Times.Once);
    }

    [Fact]
    public void ApproveLoan_MissingLinkedAccount_CoversNoDisbursementBranch()
    {
        var loan = new Loan { LoanReference = "LN1", AccountId = 1, Status = LoanStatus.Pending, PrincipalAmount = 1000m, OutstandingBalance = 1000m, InterestRate = 0.1m, TermMonths = 12, MonthlyInstalment = 87.92m };
        _loans.Setup(r => r.GetByReference("LN1")).Returns(loan);
        _accounts.Setup(r => r.GetById(1)).Returns((Account?)null);
        var result = _svc.ApproveLoan("LN1", "mgr");
        result.IsSuccess.Should().BeTrue();
        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public void ScheduleAndGetLoan_CoverMissingRejectedAndSuccessBranches()
    {
        _loans.Setup(r => r.GetByReference("missing")).Returns((Loan?)null);
        _svc.GenerateRepaymentSchedule("missing").IsSuccess.Should().BeFalse();
        _svc.GetLoan("missing").IsSuccess.Should().BeFalse();

        var rejected = new Loan { LoanReference = "rej", Status = LoanStatus.Rejected };
        _loans.Setup(r => r.GetByReference("rej")).Returns(rejected);
        _svc.GenerateRepaymentSchedule("rej").IsSuccess.Should().BeFalse();

        var active = new Loan { LoanReference = "ok", Status = LoanStatus.Active, RepaymentSchedule = new List<LoanRepayment> { new() { InstallmentNumber = 1 } } };
        _loans.Setup(r => r.GetByReference("ok")).Returns(active);
        _svc.GenerateRepaymentSchedule("ok").Data.Should().HaveCount(1);
        _svc.GetLoan("ok").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ProcessRepayment_CoversInvalidOverpaymentSettlementAndArrearsRecovery()
    {
        _loans.Setup(r => r.GetByReference("missing")).Returns((Loan?)null);
        _svc.ProcessRepayment("missing", 10m, "user").IsSuccess.Should().BeFalse();

        var pending = new Loan { LoanReference = "pending", Status = LoanStatus.Pending, OutstandingBalance = 100m };
        _loans.Setup(r => r.GetByReference("pending")).Returns(pending);
        _svc.ProcessRepayment("pending", 10m, "user").IsSuccess.Should().BeFalse();

        var active = new Loan { LoanReference = "active", Status = LoanStatus.Active, OutstandingBalance = 100m };
        _loans.Setup(r => r.GetByReference("active")).Returns(active);
        _svc.ProcessRepayment("active", 0m, "user").IsSuccess.Should().BeFalse();
        _svc.ProcessRepayment("active", 150m, "user").IsSuccess.Should().BeTrue();
        active.Status.Should().Be(LoanStatus.Settled);
        active.OutstandingBalance.Should().Be(0m);

        var arrears = new Loan { LoanReference = "arrears", Status = LoanStatus.Arrears, OutstandingBalance = 500m, ArrearsAmount = 100m, MissedPayments = 2 };
        _loans.Setup(r => r.GetByReference("arrears")).Returns(arrears);
        _svc.ProcessRepayment("arrears", 100m, "user").IsSuccess.Should().BeTrue();
        arrears.Status.Should().Be(LoanStatus.Active);
        arrears.ArrearsAmount.Should().Be(0m);
    }

    [Fact]
    public void Settlement_CoversMissingInvalidAndSuccessBranches()
    {
        _loans.Setup(r => r.GetByReference("missing")).Returns((Loan?)null);
        _svc.CalculateSettlementAmount("missing").IsSuccess.Should().BeFalse();

        var pending = new Loan { LoanReference = "pending", Status = LoanStatus.Pending, OutstandingBalance = 100m };
        _loans.Setup(r => r.GetByReference("pending")).Returns(pending);
        _svc.CalculateSettlementAmount("pending").IsSuccess.Should().BeFalse();

        var active = new Loan { LoanReference = "active", Status = LoanStatus.Active, OutstandingBalance = 1000m };
        _loans.Setup(r => r.GetByReference("active")).Returns(active);
        _svc.CalculateSettlementAmount("active").Data.Should().Be(1015m);
        var settled = _svc.SettleLoan("active", "user");
        settled.IsSuccess.Should().BeTrue();
        active.Status.Should().Be(LoanStatus.Settled);
    }
}
