# Phase 3 Research — Mock vs Stub vs Fake vs Spy (§5.5)

## Definitions

| Double | Behaviour | Interaction verification? | Typical use in BankCore tests |
|--------|-----------|---------------------------|-------------------------------|
| **Stub** | Returns canned data | No | Returning a fixed `Account` from `GetById` without caring how often it was called |
| **Mock** | Same as stub **plus** expectations on calls | **Yes** (`Times.Once`, argument matchers) | Verifying `IAuditService.Log` was called after a deposit |
| **Fake** | Working lightweight implementation | Optional | In-memory repositories in `BankCore.Data` for manual/console runs |
| **Spy** | Real object wrapped to record calls | Yes | Rarely used here; Moq mocks cover verification needs |

## Why Mock was chosen over Stub for critical scenarios

### Example — NUnit `AuditTrailTests` / Deposit audit

After a successful deposit, the business rule requires an audit event. A **stub** could return success from the repository and never prove the audit API was used.

We configured **Moq** as a **mock**:

```csharp
_audit.Verify(a => a.Log(
    "DEPOSIT",
    "teller1",
    It.Is<string>(d => d.Contains("100")),
    It.IsAny<string?>(),
    It.IsAny<bool>(),
    It.IsAny<string>()), Times.Once);
```

That fails the test if `Log` is never called or is called with the wrong event type — behaviour a pure stub would miss.

### Example — MSTest account creation failure path

```csharp
_mockAccountRepo.Verify(r => r.Add(It.IsAny<Account>()), Times.Never);
```

When validation fails, **Add must not run**. Verifying `Times.Never` is mock behaviour, not stubbing.

### Example — xUnit loan application

```csharp
_loanRepo.Verify(r => r.Add(It.Is<Loan>(l => l.PrincipalAmount == 50_000m)), Times.Exactly(1));
```

Confirms both **call count** and **argument shape**.

## When stubs were enough

Default `IValidationService` setups that always return `true` isolate account/transaction logic without asserting how many times `IsValidName` ran. That is intentional stubbing to keep tests focused on the unit under test.

## Conclusion

BankCore Phase 3 uses **stubs for collaborators that only supply data**, and **mocks wherever the specification requires a side effect** (persistence, audit, permissions). That matches the research distinction required in §5.5.
