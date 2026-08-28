# Software Test Report — BankCore Enterprise Management System

**Report phase:** Phase 4  
**Repository:** `AIGenCodePractice/qwertgyhuji`  
**Latest coverage evidence:** 2026-08-28 08:44:17  
**Report purpose:** Summarise test execution evidence, coverage exit criteria, defects and residual risk for portfolio assessment.

## 1. Scope

The test programme covers the BankCore service layer using three unit-test frameworks:

- **MSTest:** account management, authentication, validation, non-functional and support-service coverage.
- **NUnit:** deposit, withdrawal, transfer, reversal, reporting, decision-table and transaction branch tests.
- **xUnit:** interest calculation, loan application, repayment, arrears and state-transition tests.

Test design evidence includes equivalence partitioning, boundary value analysis, decision tables, state transitions, negative/robustness tests, data-driven tests and dependency isolation using Moq.

## 2. Coverage exit criteria

| Module | Required line / branch | Latest measured | Result |
|---|---:|---:|---|
| Account Management | 85% / 75% | 96.8% / 86.5% | Pass |
| Transaction Engine | 80% / 70% | 97.4% / 92.1% | Pass |
| Interest Calculator | 90% / 85% | 100% / 90.6% | Pass |
| Loan Processing | 80% / 70% | 95.8% / 90.2% | Pass |
| Authentication | 85% / 75% | 100% / 96.5% | Pass |
| Validation Library | 95% / 90% | 100% / 100% | Pass |

**Conclusion:** All six named module-specific coverage exit criteria are met in the latest archived report.

## 3. Overall quality indicators

- Overall report: **82.8% line / 83.7% branch**.
- `BankCore.Core`: **97.7% line / 90.9% branch**.
- Risk Hotspots still identify several methods with high cyclomatic complexity; this is a maintainability concern rather than evidence of failed coverage thresholds.

## 4. Defect status

The defect log records historical test/tooling issues and the remaining open product gap identified during validation testing.

- Historical test/tooling defects: recorded as **Closed** where fixes were implemented.
- `IsSafeInput` standalone SQL `OR` payload gap: retained as an **Open** product/security defect pending a production decision.
- Coverage gaps: carried as residual risks rather than falsely classified as fixed defects.

See `BankCore.TestDocs/DefectLog.xlsx`.

## 5. Residual risks

1. **Data layer coverage — High:** 0% direct execution coverage in the latest aggregate report.
2. **Reporting branch coverage — Medium:** 60.0% branch coverage.
3. **Intentional ignored test — Low:** documented `[Ignore]` demonstration remains skipped by design.
4. **Complexity hotspots — Medium:** high cyclomatic complexity remains in several core methods.

See `BankCore.TestDocs/CoverageResidualRisk.md`.

## 6. Recommendation

The named Phase 3 coverage criteria are satisfied and the Phase 4 documentation set is in place. Before final submission, the latest test suite should be executed once more after the final documentation/rubric-polish commits so the execution summary and regression log can be confirmed against the final repository revision. The next improvement priority is direct Data-layer testing, followed by targeted `ReportingService` branch tests and refactoring of high-complexity methods.

## 7. Artefact index

- Test plan: `BankCore.TestDocs/TestPlan.docx`
- Traceability matrix: `BankCore.TestDocs/TraceabilityMatrix.xlsx`
- Coverage report: `BankCore.TestDocs/Coverage/index.html`
- Phase 3 coverage analysis: `BankCore.TestDocs/Evidence/Phase3_Coverage_Analysis.md`
- Defect log: `BankCore.TestDocs/DefectLog.xlsx`
- Execution summary: `BankCore.TestDocs/Evidence/Phase4_Execution_Summary.md`
- Regression log: `BankCore.TestDocs/Evidence/Phase4_Regression_Log.md`
- Residual risk: `BankCore.TestDocs/CoverageResidualRisk.md`
