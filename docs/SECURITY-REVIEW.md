# Security Review

> Update (October 29, 2026): the historical review below is retained. A subsequent
> [local runtime and scanner assessment](docs/SECURITY-TEST-REPORT.md) reproduced
> and fixed host-validation and repository-authentication weaknesses, and records
> the logout-cookie replay issue, subsequently fixed with server-side tickets. Its results supersede the original runtime
> validation limitation below.


## Scope and limitations

This review covered the local web application source, including the provider patient controller, repository, view models, Razor views, authentication policy, CSP middleware, and `TODO.md`.

No external systems were contacted. Runtime/build validation was not available because the `dotnet` CLI is not installed in the review environment.

## Findings

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | 🟠 HIGH | [PatientsController.cs](Reflect.ProviderDashboard.Web/Areas/Provider/Controllers/PatientsController.cs#L24-L39) | 24-39 | Broken access control allows any authenticated `Mental_Health_Provider` user to list the global patient population and retrieve arbitrary patients by ID without provider, organization, tenant, or assignment authorization. | 9/10 |

### 1. Unscoped patient list and detail access

**Affected code**

- `Index` calls `PrepareData`, which performs repository-wide patient search.
- `Details` accepts any non-empty `Guid` and calls `GetByIdAsync` without an authorization scope.
- [`InMemoryPatientRepository.cs`](Reflect.ProviderDashboard.Web/Services/InMemoryPatientRepository.cs#L14-L43) filters deletion status and patient ID only; it does not filter by the signed-in provider, organization, tenant, or patient assignment.

**Exploit preconditions**

An attacker needs an authenticated account with the `Mental_Health_Provider` role and a patient ID. Patient IDs are exposed by patient-list links, so they do not need to be guessed.

**Impact**

An unauthorized provider can access another provider's or tenant's patient records, including dates of birth, notes, session history, and reflection summaries. The current repository uses synthetic data, but the same access path would expose real clinical data if connected to production storage.

**Recommended remediation**

1. Establish the authenticated provider's organization/tenant and identity from trusted server-side claims.
2. Add provider/tenant/assignment scope parameters to repository list and detail methods.
3. Enforce the scope inside the repository/query rather than filtering only in the controller.
4. Return not-found or forbidden consistently for patients outside the caller's scope without leaking whether an ID exists.
5. Add authorization tests covering:
   - a provider listing only assigned patients;
   - direct detail access to another provider's patient;
   - cross-tenant access;
   - deleted and unassigned patients;
   - tampered patient IDs.

## Remediation status

The highest-severity finding has been remediated in the local prototype by adding an explicit provider assignment field and applying the authenticated provider's ID inside every patient repository query. The repository fails closed for unauthenticated or unassigned users, and the demo account is assigned the seeded synthetic patients during startup. A production implementation should replace this in-memory assignment with a tenant-aware database relationship and authorization policy.

## Validation status

Source inspection substantiated the finding. Automated runtime/build checks could not be executed because `dotnet` is unavailable in the environment.
