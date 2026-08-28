# Phase 4 — Test Execution Summary

**System under test:** BankCore Enterprise Management System  
**Repository:** `AIGenCodePractice/qwertgyhuji`  
**Execution evidence date:** 2026-08-28  
**Coverage report timestamp:** 2026-08-28 08:44:17

## 1. Execution objective

Phase 4 records the executed test evidence, defects, regression status and final test-report conclusions using the completed Phase 3 test suites and latest archived coverage report.

## 2. Execution environment

- .NET solution: `BankCoreSolution.sln`
- Test frameworks: MSTest, NUnit and xUnit
- Coverage collector: `XPlat Code Coverage`
- Report tool: `reportgenerator`
- Coverage inputs: 18 Cobertura reports

## 3. Latest coverage execution result

| Scope | Line coverage | Branch coverage | Status |
|---|---:|---:|---|
| Overall archived report | 82.8% | 83.7% | Evidence captured |
| `BankCore.Core` | 97.7% | 90.9% | Strong |
| AccountService | 96.8% | 86.5% | Threshold met |
| TransactionService | 97.4% | 92.1% | Threshold met |
| InterestCalculator | 100% | 90.6% | Threshold met |
| LoanService | 95.8% | 90.2% | Threshold met |
| AuthService | 100% | 96.5% | Threshold met |
| ValidationService | 100% | 100% | Threshold met |

## 4. Execution conclusion

All six named module-specific Phase 3 coverage exit criteria are met in the latest archived report. No module remains marked as below its specified line or branch threshold.

## 5. Outstanding execution risks

1. `BankCore.Data` is not directly executed in the latest aggregated coverage report and remains at 0% execution coverage.
2. `ReportingService` remains at 60.0% branch coverage despite 97.0% line coverage.
3. One documented MSTest `[Ignore]` demonstration is intentionally skipped and is retained as rubric/framework evidence.
4. High-complexity methods remain maintainability hotspots even where their CRAP score indicates strong execution coverage.

## 6. Related Phase 4 artefacts

- Defect log: `BankCore.TestDocs/DefectLog.xlsx`
- Regression log: `BankCore.TestDocs/Evidence/Phase4_Regression_Log.md`
- Software test report: `BankCore.TestDocs/Evidence/Phase4_Software_Test_Report.md`
- Coverage analysis: `BankCore.TestDocs/Evidence/Phase3_Coverage_Analysis.md`
- Residual risk: `BankCore.TestDocs/CoverageResidualRisk.md`
