# Phase 3 Research — PMT / Amortisation Formula (§5.4.2)

## Purpose

`LoanService` builds repayment schedules and monthly instalments. Testers must verify schedule output against the standard amortising loan formula.

## Formula

Monthly rate:

\[
r = \frac{R}{12}
\]

where \(R\) is the nominal annual interest rate (e.g. \(0.12\) for 12%).

Payment for principal \(P\) over \(n\) months:

\[
\mathrm{PMT} = P \cdot \frac{r(1+r)^{n}}{(1+r)^{n} - 1}
\]

When \(r = 0\):

\[
\mathrm{PMT} = \frac{P}{n}
\]

## Derivation (brief)

1. Each period, interest is charged on outstanding balance, then instalment reduces principal.
2. The closed form above is the annuity formula that keeps the instalment **constant** while principal and interest portions change over time.
3. Schedule row \(k\): interest portion \(\approx\) balance \(× r\); principal portion \(= \mathrm{PMT} -\) interest; new balance \(=\) old balance \(-\) principal portion.

## How tests use it

- **RepaymentScheduleTests** — after approve/disburse, schedule length equals `TermMonths` (e.g. 36).
- **LoanApplicationTests** — application stores `MonthlyInstalment` from service PMT helper.
- **EarlySettlementTests** — settlement amount is outstanding + fee (documents BUG-012 fee-on-principal behaviour).

## Worked numeric check (manual)

Example: \(P = 12000\), \(R = 12\%\) → \(r = 0.01\), \(n = 12\):

\[
\mathrm{PMT} = 12000 \cdot \frac{0.01(1.01)^{12}}{(1.01)^{12}-1} \approx 1066.19
\]

Testers compare service output to this order of magnitude (and FluentAssertions/`BeApproximately` where exact decimals are asserted).

## Reference

- Standard amortising loan / annuity payment (ISTQB-related finance testing practice; Excel `PMT` function equivalent).
