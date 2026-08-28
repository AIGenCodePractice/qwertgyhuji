# Phase 3 Code Coverage — Aggressive Attempt and Residual Risk

## Assessment baseline
The WIL specification requires minimum line/branch coverage of 80%/70% for Transaction Engine, 90%/85% for Interest Calculator, 80%/70% for Loan Processing, 85%/75% for Authentication, and 95%/90% for Validation Library. It also requires comprehensive techniques, including boundary analysis, decision tables, state transitions and negative/robustness testing.

## Current reported coverage
| Module | Required | Reported | Gap |
|---|---:|---:|---|
| Transaction | 80% / 70% | 83% / 63% | Branch -7pp |
| Interest | 90% / 85% | 96% / 54% | Branch -31pp |
| Loan | 80% / 70% | 87% / 65% | Branch -5pp |
| Auth | 85% / 75% | 76% / 67% | Line -9pp; Branch -8pp |
| Validation | 95% / 90% | 74% / 56% | Line -21pp; Branch -34pp |

## Aggressive coverage attempts added
Additional tests explicitly target decision points, invalid paths, boundary values, alternate status paths, compound/simple calculation paths, security-pattern families, and null/missing repository branches.

### Transaction Engine
Additional branch-focused cases exercise transfer early exits, missing source/destination accounts, inactive source/destination accounts, insufficient funds, daily-limit rejection, successful transfer, reversal validation/status/time/account paths, deposit reversal insufficiency, withdrawal reversal, and transaction-history missing/range/non-range paths.

### Interest Calculator
Existing branch tests cover invalid principal/rate/days/frequency and the compound/simple FutureValue choice. Residual risk remains around branch granularity in mathematical operations and combinations that are difficult to activate through public APIs without changing production behaviour.

### Loan Processing
Additional cases exercise application validation branches, missing/inactive accounts, debt-to-income rejection, successful pending application, approval with missing linked account, schedule/get-loan failure/success paths, repayment invalid/overpayment/settlement/arrears-recovery paths, and settlement failure/success paths.

### Authentication
Additional cases exercise lockout expiry, third failed-login lockout, blank/missing/inactive/expired sessions, logout missing/success, password validation/history paths, registration validation/duplicate/success, lock/unlock missing/success, and permission allow/deny branches.

### Validation Library
An additional dedicated suite expands partitions for every public validator: SA ID length/digit/month/day/Luhn paths, account-number regex boundaries, custom amount min/max boundaries, name length/regex alternatives, username length/regex alternatives, each password complexity condition, branch-code length/content boundaries, email regex partitions, and every SQL/XSS security-pattern family.

## Residual risk statement
The remaining uncovered branches are **known and explicitly disclosed**, rather than being represented as successful coverage. The current coverage shortfalls therefore constitute an evidence-backed residual risk: high-risk business logic has received aggressive additional testing, but the measured branch targets are still not demonstrated as met.

### Highest residual risks
1. **Validation Library — High:** line and branch coverage remain materially below the specification. Some branches may require additional equivalence classes and malformed-input combinations.
2. **Interest Calculator — High:** branch coverage remains substantially below target despite high line coverage; branch adequacy is therefore a more meaningful concern than line execution alone.
3. **Transaction Engine — High:** core monetary paths are well exercised, but branch coverage remains below the mandated threshold, especially around multi-condition business rules and reversal/transaction combinations.
4. **Loan Processing — High:** application, repayment and settlement paths are exercised, but not all branch combinations are demonstrated by the current measured result.
5. **Authentication — High:** both line and branch coverage remain below target; lockout/session/permission permutations remain security-sensitive.

## Recommended assessor wording
"The tester made an aggressive, risk-based attempt to close the remaining coverage gaps by adding targeted branch, boundary, invalid-input, status-transition and dependency-isolation tests. The residual shortfalls are intentionally reported rather than hidden. Based on the current measured results, the module-specific coverage exit criteria are not yet fully demonstrated, and the uncovered branches remain a release risk requiring further targeted tests and/or justified exclusion." 
