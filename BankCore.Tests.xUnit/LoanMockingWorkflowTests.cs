using BankCore.Core.Interfaces;
using BankCore.Core.Models;
using BankCore.Core.Services;
using FluentAssertions;
using Moq;

namespace BankCore.Tests.xUnit;

/// <summary>
/// Phase 3 Moq workflow tests for loan approval/rejection dependencies.
/// The repository mock isolates LoanService while production behavior is exercised.
/// </summary>
public class LoanMockingWorkflowTests
{
    [Fact]
    public void RejectLoan_PendingLoan_UpdatesRepositoryAndWritesAuditEvent()
    {
        var loan = new Loan
        {
            LoanReference = "LN-REJECT-001",
            Status = LoanStatus.Pending,
            PrincipalAmount = 25_000m,
            OutstandingBalance = 25_000m
        };

        var loanRepo = new Mock<ILoanRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var audit = new Mock<IAuditService>();
        loanRepo.Setup(r => r.GetByReference("LN-REJECT-001")).Returns(loan);
        loanRepo.Setup(r => r.Update(It.IsAny<Loan>()));

        var service = new LoanService(loanRepo.Object, accountRepo.Object, audit.Object);
        var result = service.RejectLoan("LN-REJECT-001", "Income verification failed", "manager1");

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Status.Should().Be(LoanStatus.Rejected);
        loanRepo.Verify(r => r.GetByReference("LN-REJECT-001"), Times.Once);
        loanRepo.Verify(r => r.Update(It.Is<Loan>(l =>
            l.LoanReference == "LN-REJECT-001" &&
            l.Status == LoanStatus.Rejected)), Times.Once);
        audit.Verify(a => a.Log(
            "LOAN_REJECTED",
            "manager1",
            It.Is<string>(message =>
                message.Contains("LN-REJECT-001") &&
                message.Contains("Income verification failed")),
            "LN-REJECT-001",
            true,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void RejectLoan_WhenRepositoryUpdateFails_PropagatesConfiguredException()
    {
        var loan = new Loan
        {
            LoanReference = "LN-REJECT-ERR",
            Status = LoanStatus.Pending
        };

        var loanRepo = new Mock<ILoanRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var audit = new Mock<IAuditService>();
        loanRepo.Setup(r => r.GetByReference("LN-REJECT-ERR")).Returns(loan);
        loanRepo.Setup(r => r.Update(It.IsAny<Loan>()))
            .Throws(new InvalidOperationException("Loan database unavailable"));

        var service = new LoanService(loanRepo.Object, accountRepo.Object, audit.Object);

        var action = () => service.RejectLoan("LN-REJECT-ERR", "System check", "manager1");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Loan database unavailable");
        loanRepo.Verify(r => r.GetByReference("LN-REJECT-ERR"), Times.Once);
        loanRepo.Verify(r => r.Update(It.Is<Loan>(l => l.Status == LoanStatus.Rejected)), Times.Once);
        audit.Verify(a => a.Log(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<bool>(),
            It.IsAny<string>()), Times.Never);
    }
}
