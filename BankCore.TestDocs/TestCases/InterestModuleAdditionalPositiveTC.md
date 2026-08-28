# Interest Module — Additional Positive Test Case

This addendum adds one further positive Interest-module test case so that the written positive-test minimum is fully met.

| TC ID | Requirement / Method | Test Type | Test Data | Expected Result |
|---|---|---|---|---|
| INT-POS-06 | `InterestCalculator.FutureValue` — valid simple-interest calculation (`isCompound = false`) | Positive / functional | Principal = `10000.00`; annual rate = `0.10`; months = `12`; `isCompound = false` | Future value = `11000.00`; calculation completes without exception |

**Design rationale:** This is a valid positive scenario and explicitly exercises the `FutureValue` simple-interest branch. It complements the existing positive interest scenarios by demonstrating the successful non-compound path with a full 12-month period.
