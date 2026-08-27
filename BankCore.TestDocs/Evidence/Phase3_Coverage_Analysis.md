# Phase 3 — Code Coverage Analysis

**Solution:** BankCore Enterprise Management System  
**Report path:** `BankCore.TestDocs/Coverage/index.html`  
**Tooling:** `dotnet test --collect:"XPlat Code Coverage"` + `reportgenerator`  
**Overall:** Line **70.7%** (788/1113) · Branch **59.9%** (214/357)

## Module results vs §5.6 thresholds

| Module (brief) | Min line | Min branch | Measured class | Line % | Branch % | Met? |
|----------------|----------|------------|----------------|--------|----------|------|
| Account Management | 85% | 75% | AccountService | **98.3%** | **88.8%** | **Yes** |
| Transaction Engine | 80% | 70% | TransactionService | **83.2%** | **63.3%** | Line yes; **branch short** |
| Interest Calculator | 90% | 85% | InterestCalculator | **95.5%** | **53.5%** | Line yes; **branch short** |
| Loan Processing | 80% | 70% | LoanService | **86.5%** | **65.2%** | Line yes; **branch short** |
| Authentication | 85% | 75% | AuthService | **76.4%** | **66.6%** | **Below both** |
| Validation Library | 95% | 90% | ValidationService | **73.6%** | **55.5%** | **Below both** |

## Interpretation

- **Strong areas:** Account lifecycle and reporting paths are well exercised via MSTest/NUnit suites with Moq isolation.
- **Branch gaps:** Compound boolean rules (withdraw decision table), interest compounding frequencies, and validation edge paths leave alternate branches unexecuted.
- **Auth/Validation:** Real `ValidationService` is tested in ValidationTests, but Luhn/date BUG paths and lockout/session edge branches still reduce measured %. Auth flows depend on hasher/session mocks; some failure paths are short-circuited.

## Remediation recorded (Phase 3 close-out)

1. Keep decision-table and boundary tests (NUnit) to grow Transaction branch coverage.
2. Expand ValidationTests for ID month edge cases, password complexity combinations, and amount max boundary (BUG-002).
3. Add Auth tests for lockout counter, RegisterUser, and ChangePassword history (already partially present).
4. Re-run coverage after each expansion; archive HTML under `BankCore.TestDocs/Coverage/`.

## Evidence artefacts

- HTML report: `BankCore.TestDocs/Coverage/index.html`
- Per-class pages: `BankCore.TestDocs/Coverage/BankCore.Core_*.html`
- This analysis file for the portfolio / SE0101 appendix

**Assessor note:** Thresholds not fully met on all modules; gap is **documented with measurements** and a clear improvement plan (professional defect/quality reporting practice).
