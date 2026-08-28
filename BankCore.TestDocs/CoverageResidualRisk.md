# Phase 3 Code Coverage — Residual Risk

## Assessment baseline
The WIL specification requires minimum line/branch coverage of 80%/70% for Transaction Engine, 90%/85% for Interest Calculator, 80%/70% for Loan Processing, 85%/75% for Authentication, and 95%/90% for Validation Library.

## Latest measured result
**Coverage report date:** 2026-08-28 08:44:17  
**Overall report:** 82.8% line · 83.7% branch  
**Core assembly:** 97.7% line · 90.9% branch

| Module | Required | Latest measured | Exit status |
|---|---:|---:|---|
| Transaction | 80% / 70% | **97.4% / 92.1%** | **Met** |
| Interest | 90% / 85% | **100% / 90.6%** | **Met** |
| Loan | 80% / 70% | **95.8% / 90.2%** | **Met** |
| Auth | 85% / 75% | **100% / 96.5%** | **Met** |
| Validation | 95% / 90% | **100% / 100%** | **Met** |

## Residual risk statement
The specified module coverage thresholds are now met. Residual risk has therefore shifted away from the original Transaction/Interest/Loan/Auth/Validation shortfalls.

### 1. Data layer — High residual coverage risk
The latest summary reports `BankCore.Data` with **0 covered lines from 169 coverable lines** and **0 covered branches from 30 total branches**. This is the main reason the overall solution coverage is materially lower than the `BankCore.Core` coverage.

**Risk:** repository and seeding behaviour is not demonstrated as directly executed by the latest aggregated coverage run.

**Recommended treatment:** add direct Data-layer tests or explicitly justify the layer boundary if the assessment scope intentionally measures service-layer logic only.

### 2. ReportingService branch coverage — Medium residual risk
`ReportingService` is measured at **97.0% line coverage** but only **60.0% branch coverage** (12/20 branches).

**Risk:** alternate statement-generation combinations remain less thoroughly demonstrated than the named threshold modules.

**Recommended treatment:** add date-range, missing-account, empty-transaction, ordering and filtering combinations to close the remaining branches.

### 3. Intentional exclusions — Low residual risk
The MSTest suite contains a documented `[Ignore(...)]` demonstration required for framework/rubric evidence. An ignored test is intentionally skipped and therefore does not contribute execution coverage.

**Risk:** the exclusion must remain documented so it is not mistaken for an unexecuted defect test.

**Recommended treatment:** retain the reason on the attribute and keep the exclusion visible in the test report.

### 4. Complexity hotspots — Maintainability risk
Several methods remain highly complex even where coverage is strong, including `LoanService.ApplyForLoan`, `TransactionService.ReverseTransaction`, `ValidationService.IsValidSouthAfricanIdNumber`, `ReportingService.GenerateStatement`, `TransactionService.Transfer` and `AuthService.Login`.

**Risk:** future changes may be difficult to maintain because high cyclomatic complexity remains.

**Recommended treatment:** consider production refactoring into smaller decision units; this is a maintainability improvement rather than a current coverage exit failure.

## Conclusion
**All named Phase 3 coverage thresholds are met in the latest report.** The residual risk is now explicitly limited to the **Data layer's 0% execution coverage, ReportingService branch coverage, intentional exclusions, and maintainability hotspots**.
