# Phase 3 — Code Coverage Analysis

**Solution:** BankCore Enterprise Management System  
**Report path:** `BankCore.TestDocs/Coverage/index.html`  
**Tooling:** `dotnet test --collect:"XPlat Code Coverage"` + `reportgenerator`  
**Latest report coverage date:** **2026-08-28 08:44:17**  
**Report parser:** MultiReport (**18 Cobertura reports**)  
**Overall report:** Line **82.8%** (917/1107) · Branch **83.7%** (320/382)

> The overall percentage includes `BankCore.Data`, which is currently reported at 0% execution coverage. The assessed business modules below are measured at the concrete service-class level.

## Module results vs §5.6 thresholds

| Module | Min line | Min branch | Measured class | Line % | Branch % | Met? |
|---|---:|---:|---|---:|---:|---|
| Account Management | 85% | 75% | AccountService | **96.8%** | **86.5%** | **Yes** |
| Transaction Engine | 80% | 70% | TransactionService | **97.4%** | **92.1%** | **Yes** |
| Interest Calculator | 90% | 85% | InterestCalculator | **100%** | **90.6%** | **Yes** |
| Loan Processing | 80% | 70% | LoanService | **95.8%** | **90.2%** | **Yes** |
| Authentication | 85% | 75% | AuthService | **100%** | **96.5%** | **Yes** |
| Validation Library | 95% | 90% | ValidationService | **100%** | **100%** | **Yes** |

## Interpretation

- **All specified §5.6 module thresholds are met** in the latest report.
- `BankCore.Core` overall coverage is **97.7% line** and **90.9% branch**.
- The current report's main remaining coverage risk is the **Data layer**, which is aggregated at **0% execution coverage** in the report.
- `ReportingService` remains a branch-coverage hotspot at **97.0% line / 60.0% branch**. Reporting is not one of the six named §5.6 threshold rows, but the gap remains relevant as residual technical risk.
- High cyclomatic-complexity methods remain visible in the Risk Hotspots view even where coverage is strong. Complexity is therefore documented as a maintainability risk rather than misreported as a threshold failure.

## Remediation completed in Phase 3

1. Added targeted branch, boundary, state and negative-path tests for Transaction, Interest, Loan, Authentication and Validation.
2. Added direct concrete coverage for `AuditService` and `PasswordHasher` after the coverage report identified them as 0%.
3. Repaired MSTest compatibility so the existing MSTest 4 assertion APIs remain supported.
4. Added data-driven MSTest coverage using `[DataTestMethod]` / `[DataRow]` in the aggressive validation suite.
5. Re-ran and archived the latest HTML report under `BankCore.TestDocs/Coverage/`.

## Evidence artefacts

- HTML report: `BankCore.TestDocs/Coverage/index.html`
- Per-class pages: `BankCore.TestDocs/Coverage/BankCore.Core_*.html`
- Residual risk: `BankCore.TestDocs/CoverageResidualRisk.md`
- Phase 3 checklist: `BankCore.TestDocs/Evidence/Phase3_Checklist.md`

**Assessor note:** The latest measured result demonstrates that all six named module-specific coverage exit criteria are met. Remaining concerns are reported as **residual risk** rather than as unresolved threshold failures.
