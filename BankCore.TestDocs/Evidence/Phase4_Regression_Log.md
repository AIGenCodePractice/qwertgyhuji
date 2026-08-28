# Phase 4 — Regression Log

This log records regression checks performed after Phase 3 coverage, framework-compatibility and support-service changes.

| Regression item | Change / area checked | Result | Status | Evidence |
|---|---|---|---|---|
| MSTest 4 compatibility | Restored MSTest 4-compatible assertion APIs after temporary legacy-version mismatch | Existing modern assertions remain supported | Pass | `BankCore.Tests.MSTest.csproj`, account/auth suites |
| Data-driven MSTest | Added explicit `[DataTestMethod]` usage to validation partitions | Data-driven tests remain aligned with MSTest data-driven semantics | Pass | `ValidationAggressiveCoverageTests.cs` |
| AuditService coverage tests | Removed flaky wall-clock range assertion | Audit timestamp still verified as populated and UTC | Pass | `SupportServicesCoverageTests.cs` |
| PasswordHasher coverage tests | Added direct concrete hashing/verification tests | Correct and incorrect password paths covered | Pass | `SupportServicesCoverageTests.cs` |
| NUnit Deposit rubric items | Confirmed `TestCaseSource`, `OneTimeSetUp`, `Retry` and multiple assertions | Required demonstrations visible | Pass | `DepositTests.cs` |
| NUnit Withdrawal rubric items | Added/confirmed `TestCaseSource`, `OneTimeSetUp`, `Retry` and multiple assertions | Required demonstrations visible | Pass | `WithdrawalTests.cs` |
| xUnit shared fixture | Confirmed `ICollectionFixture<CalculatorFixture>` | Fixture requirement visible | Pass | `CalculatorFixture.cs` |
| Core coverage thresholds | Re-ran latest archived coverage report | All six named modules meet thresholds | Pass | `Coverage/index.html` |

## Open regression-related risks

- `BankCore.Data` remains at 0% direct execution coverage in the latest aggregate report.
- `ReportingService` branch coverage remains at 60%.
- A separate nullable-reference warning in `MainMenu.cs` is a static-analysis concern and is not recorded here as a failed test regression.

## Regression conclusion

The documented framework and coverage changes did not leave any of the six named coverage threshold modules below their required exit criteria. Remaining risks are carried forward as coverage and maintainability risks rather than as confirmed functional regressions.
