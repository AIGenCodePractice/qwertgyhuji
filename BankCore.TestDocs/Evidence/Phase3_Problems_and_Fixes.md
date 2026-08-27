# Phase 3 — Problems Encountered and How They Were Fixed

Evidence log of defects in the **test harness and tooling** (not only product bugs).  
Screenshots were captured during development in Visual Studio / Error List / Test Explorer; this document records the same findings for the portfolio.

---

## 1. MSTest `DataRow` parameter binding failure

**Symptom (Test Explorer):**  
`Cannot run test method ... Test data doesn't match method parameters` on `CreateAccount_WithVariousTypes` and `CreateAccount_WithInvalidInput`.

**Cause:**  
`[DataRow]` supplied `int` / enum values into `decimal` / `AccountType` parameters; MSTest host rejected the conversion.

**Fix:**  
Use `double` for money and `int` ordinals for account type, then cast inside the test:

```csharp
[DataRow("Alice Smith", "9876543210987", 100.0, 0)] // Savings
public void CreateAccount_WithVariousTypes_...(string name, string id, double deposit, int typeOrdinal)
{
    var type = (AccountType)typeOrdinal;
    var depositAmt = (decimal)deposit;
    ...
}
```

**Status:** Fixed in `AccountCreationTests.cs`.

---

## 2. Expected exception never thrown (null ID)

**Symptom:**  
`Assert.ThrowsExactly<ArgumentNullException>` failed — **no exception was thrown**.

**Cause:**  
`AccountService.CreateAccount` returns `OperationResult.Failure` after validation; it does **not** throw on null ID.

**Fix:**  
Align test with production behaviour: assert `IsSuccess == false` and `Add` never called. Keep a separate `ThrowsExactly` demo only for true throw paths / Phase 3 attribute demo.

**Status:** Fixed (`CreateAccount_NullIdNumber_ReturnsFailure`).

---

## 3. Password change with invalid session still succeeded

**Symptom:**  
`ChangePassword_InvalidSession_ReturnsFailure` — expected `IsSuccess == false`, actual `true`.

**Cause:**  
Session mock used `GetByToken(It.IsAny<string>()).Returns(session)`, so **every** token looked valid.

**Fix:**  
Return session only for the real token; unknown tokens return `null`. Rebuild `AuthService` in the test with that setup.

**Status:** Fixed in `PasswordPolicyTests.cs`.

---

## 4. SQL injection test expected false, got true

**Symptom:**  
`IsSafeInput("1' OR '1'='1")` returned **true**.

**Cause:**  
Production `IsSafeInput` only flags a fixed keyword list (`SELECT`, `UNION`, `DROP`, `--`, `<`, `>`, …). Standalone `OR` is not in that list (product gap / limited sanitiser).

**Fix:**  
- Parameterized tests use payloads that **do** hit keywords (`UNION SELECT`, `DROP`, `<script>`).  
- Separate test **documents** the OR-payload gap for the defect log.

**Status:** Tests aligned with production; gap recorded for Phase 4 defects.

---

## 5. Moq CS0854 — optional arguments in expression trees

**Symptom (Error List):**  
`An expression tree may not contain a call or invocation that uses optional arguments` on `IAuditService.Log(...)`.

**Cause:**  
Setup/Verify omitted optional parameters (`isSuccessful`, `ipAddress`).

**Fix:**  
Always pass all six arguments with `It.IsAny<...>()` in Moq expressions.

**Status:** Fixed in DepositTests / AccountCreationTests audit verifies.

---

## 6. NUnit CS0121 — Assert.Multiple / Assert.Throws ambiguity

**Symptom:**  
Ambiguous overload between `TestDelegate` and `System.Action`; also `Framework` resolution issues under namespace `BankCore.Tests.NUnit`.

**Fix:**  
- Prefer sequential `Assert.That` where sufficient.  
- Where Multiple is required: `Assert.Multiple((global::System.Action)(() => { ... }))`.  
- For throws: `Assert.Throws<T>((TestDelegate)(() => ...))`.

**Status:** Fixed in WithdrawalTests / TransferTests / DepositTests.

---

## 7. NUnit `[Values]` on method (CS0592 / NUnit1020)

**Symptom:**  
`Values` attribute not valid on method; test has parameters but no arguments supplied.

**Fix:**  
Place `[Values(...)]` on the **parameter**:

```csharp
public void Deposit_CombinedWithValues_MultipleAmounts([Values(100, 250, 500)] decimal amount)
```

**Status:** Fixed.

---

## 8. Obsolete `[Timeout]` (CS0618)

**Symptom:**  
Timeout attribute obsolete — prefer cooperative cancellation.

**Fix:**  
Use `[CancelAfter(2000)]` on performance-style tests.

**Status:** Fixed.

---

## 9. FluentAssertions decimal API (xUnit)

**Symptom:**  
`NotBeNaN` invalid for `decimal`; `Be(..., precision:)` invalid; `double` passed where `decimal` required.

**Fix:**  
`BeApproximately(expected, 0.01m)`; use `1000m` / `0.08m` literals; drop `NotBeNaN` for decimals.

**Status:** Fixed in `InterestCalculationTheories.cs`.

---

## 10. Coverage / ReportGenerator CLI failure

**Symptom:**  
`MSB1003` / no `coverage.cobertura.xml` when run from wrong directory (`...\source\repos` without solution).

**Fix:**  
Run from solution root:

```powershell
cd <path-to-BankCoreSolution>
dotnet test BankCoreSolution.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"BankCore.TestDocs/Coverage" -reporttypes:Html
```

**Status:** Coverage HTML committed under `BankCore.TestDocs/Coverage/`.

---

## 11. Git remote / branch confusion

**Symptom:**  
Work appeared local-only; public `BankTest` still showed stubs; push targeted wrong branch name (`master` vs `main`).

**Fix:**  
Push complete tree to dedicated assessment repo `qwertgyhuji` on `main`; document remote URL for assessors.

**Status:** This repository contains the Phase 3 deliverable set.

---

## Summary

| # | Problem class | Resolution type |
|---|---------------|-----------------|
| 1–4 | Test design vs production behaviour | Corrected assertions / mocks |
| 5–9 | Framework / compiler API misuse | Code fixes in test projects |
| 10–11 | Tooling / process | Documented CLI + correct remote |

These items form **Phase 3 supporting evidence** (WA0103/WA0104 style): problems were detected, analysed, fixed, and recorded — not only “all tests green on first run.”
