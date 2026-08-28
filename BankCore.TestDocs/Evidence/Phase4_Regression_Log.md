# Phase 4 — Regression Log

This log records regression evidence after Phase 3 coverage, framework-compatibility and support-service changes. It distinguishes **executed evidence already present in the archived report** from **final repository polish changes that still require one final execution run**.

| Regression item | Change / area checked | Result | Status | Evidence |
|---|---|---|---|---|
| MSTest 4 compatibility | Restored MSTest 4-compatible assertion APIs after temporary legacy-version mismatch | Existing modern assertions are compatible with the project version | Verified in source; latest report predates final polish | Project/test source |
| Data-driven MSTest | Added explicit `[DataTestMethod]` usage to validation partitions | Literal rubric syntax added without changing test inputs | Pending final execution | `ValidationAggressiveCoverageTests.cs` |
| AuditService coverage tests | Removed flaky wall-clock range assertion | Timestamp assertion now checks populated UTC value | Verified by latest archived coverage change history | `SupportServicesCoverageTests.cs` |
| PasswordHasher coverage tests | Added direct concrete hashing/verification tests | Correct and incorrect password paths targeted | Verified by latest archived coverage change history | `SupportServicesCoverageTests.cs` |
| NUnit Deposit rubric items | Confirmed `TestCaseSource`, `OneTimeSetUp`, `Retry` and multiple assertions | Required demonstrations visible | Verified in source | `DepositTests.cs` |
| NUnit Withdrawal rubric items | Added `TestCaseSource` and `Retry`; retained `OneTimeSetUp` and multiple assertions | Required demonstrations visible | Pending final execution | `WithdrawalTests.cs` |
| xUnit shared fixture | Confirmed `ICollectionFixture<CalculatorFixture>` | Fixture requirement visible | Verified in source | `CalculatorFixture.cs` |
| Core coverage thresholds | Latest archived coverage report | All six named modules meet thresholds | Executed evidence | `Coverage/index.html` |

## Open regression-related risks

- `BankCore.Data` remains at 0% direct execution coverage in the latest aggregate report.
- `ReportingService` branch coverage remains at 60%.
- A separate nullable-reference warning in `MainMenu.cs` is a static-analysis concern and is not recorded here as a failed test regression.

## Regression conclusion

The latest executed coverage report demonstrates that none of the six named threshold modules is below its required exit criterion. Two final rubric-polish changes were made after that report (`[DataTestMethod]` syntax and additional Withdrawal framework demonstrations), so one final test run should be performed before submission to confirm the final repository revision. Remaining risks are carried forward as coverage and maintainability risks rather than as confirmed functional regressions.
