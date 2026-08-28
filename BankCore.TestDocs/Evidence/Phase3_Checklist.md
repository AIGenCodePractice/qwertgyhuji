# Phase 3 — Completion Checklist

**Repository:** https://github.com/AIGenCodePractice/qwertgyhuji.git  
**Related evidence folder:** `BankCore.TestDocs/Evidence/`

## Framework implementation (§5.1–5.4)

- [x] MSTest: Account + Auth + Validation + TestDataHelper + Moq helpers
- [x] NUnit: Transactions + Reporting + Decision table (8 columns)
- [x] xUnit: Interest theories + Loan suites + `ICollectionFixture` + DecimalPrecisionComparer
- [x] Data-driven tests (`DataTestMethod` / `DataRow` / `TestCase` / `TestCaseSource` / `InlineData` / `MemberData` / `ClassData`)
- [x] Setup/teardown (`TestInitialize`, `SetUp`, `OneTimeSetUp`, fixtures)
- [x] Categories, Ignore, Retry, CancelAfter, multiple assertions, exception demonstrations
- [x] Moq verification (`Times.Once` / `Exactly` / `Never`) across frameworks

## Coverage (§5.6)

- [x] `dotnet test --collect:"XPlat Code Coverage"` executed
- [x] HTML report in `BankCore.TestDocs/Coverage/`
- [x] Latest report date: **2026-08-28 08:44:17**
- [x] Analysis vs thresholds: `Evidence/Phase3_Coverage_Analysis.md`
- [x] **All six named module thresholds meet the required line and branch coverage exit criteria**
- [x] Residual risks documented: Data layer 0%, ReportingService branch coverage, intentional exclusions and complexity hotspots

## Research tasks

- [x] Mock vs Stub justification: `Evidence/Phase3_Mock_vs_Stub.md`
- [x] PMT formula note: `Evidence/Phase3_PMT_Formula.md`

## Problems & fixes evidence

- [x] `Evidence/Phase3_Problems_and_Fixes.md` (maps Error List / Test Explorer issues to fixes)

## Assessor quick links

| Artefact | Path |
|----------|------|
| Coverage index | `BankCore.TestDocs/Coverage/index.html` |
| Coverage analysis | `BankCore.TestDocs/Evidence/Phase3_Coverage_Analysis.md` |
| Residual risk | `BankCore.TestDocs/CoverageResidualRisk.md` |
| Mock/Stub | `BankCore.TestDocs/Evidence/Phase3_Mock_vs_Stub.md` |
| PMT | `BankCore.TestDocs/Evidence/Phase3_PMT_Formula.md` |
| Problems/fixes | `BankCore.TestDocs/Evidence/Phase3_Problems_and_Fixes.md` |
| Test plan (Phase 1) | `BankCore.TestDocs/TestPlan.docx` |
| RTM (Phase 2) | `BankCore.TestDocs/TraceabilityMatrix.xlsx` |
