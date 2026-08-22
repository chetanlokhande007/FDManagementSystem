# FinTrust FD Manager — Project Context

**Last Updated**: August 22, 2026
**Branch**: feature/fd-cashflow-recalculation
**Status**: P0 and P1 fixes applied, builds clean (0 errors, 0 warnings)

---

## 1. Project Overview

**FinTrust FD Manager** is a Fixed Deposit management system for financial institutions.

| Component | Technology | Location |
|-----------|-----------|----------|
| Backend API | .NET 8 Web API | `Backend/FinTrustFDManager.API/` |
| Business Logic | .NET 8 Class Library | `Backend/FinTrustFDManager.BAL/` |
| Data Access | EF Core + PostgreSQL | `Backend/FinTrustFDManager.DAL/` |
| Domain Models | .NET 8 Class Library | `Backend/FinTrustFDManager.Model/` |
| Frontend | Angular + TypeScript | `FinTrustFDManager.UI/` |

### Architecture: N-Tier
```
Angular UI → API Controllers → BAL Services → DAL Repositories → EF Core → PostgreSQL
```

### Two Parallel Systems (Legacy + Active)
- **Legacy**: `Investment` / `CashFlow` entities (CoreData) — appears unused by frontend
- **Active (FD System)**: `FDIdentification` / `FDInterest` / `FDCashFlow` entities — the main system

---

## 2. Key Business Flow

1. User creates FD Identification (entity, counterparty, currency, principal, dates)
2. User configures FD Interest (rate type, rate, frequency, compounding, calculation basis)
3. Backend generates cash flow schedule automatically
4. User views cash flows in a table with totals

### Cash Flow Events
| Event | Description |
|-------|-------------|
| `FD Created` | Initial investment (OUTFLOW) |
| `Interest` | Periodic interest payment (INFLOW) |
| `Compounding Interest` | Interest reinvested into principal (INFLOW, no cash movement) |
| `Maturity` | Final payout of principal + all interest (INFLOW) |

---

## 3. Fixes Applied (This Session)

### P0 — Critical Fixes

| # | Fix | Files Changed |
|---|-----|--------------|
| 1 | **Created missing DashboardService/Repository** | `DAL/Repositories/DashboardRepository.cs` (NEW), `BAL/Services/DashboardService.cs` (NEW), `BAL/Common/DependencyInjection.cs`, `API/Program.cs` |
| 2 | **Removed hardcoded credentials** | `API/appsettings.json` — replaced DB password and JWT key with `${DB_PASSWORD}` and `${JWT_SECRET_KEY}` placeholders |
| 3 | **Rewrote GetNextScheduleDate** | `BAL/Services/FDInterestService.cs` — schedule dates now anchor from start date (was calendar-boundary-based). Added `AddPeriodWithEomHandling()` for EOM semantics |
| 4 | **Removed Npgsql legacy timestamp** | `API/Program.cs` — removed `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` |
| 5 | **Fixed hardcoded frontend URLs** | 13 service files updated to use `environment.apiUrl` instead of `http://localhost:5075/api/...` |
| 6 | **Removed duplicate DI registrations** | `API/Program.cs` — removed duplicate `IFDInterestRepository` and `IFDInterestService` registrations |

### P1 — High Priority Fixes

| # | Fix | Files Changed |
|---|-----|--------------|
| 7 | **Re-enabled [Authorize] on all controllers** | 8 controllers uncommented: Investment, InvestmentApproval, InterestFrequency, DayCountConvention, Currency, CashFlow, Bank, CounterParty |
| 8 | **Added AsNoTracking to FDCashFlow GetByFdIdAsync** | `DAL/Repositories/FDCashFlowRepository.cs` |
| 9 | **Fixed sequential delete loops** | `BAL/Services/FDIdentificationService.cs`, `BAL/Services/FDInterestService.cs` — replaced foreach+DeleteAsync with DeleteRangeAsync |
| 10 | **Added DeleteRangeAsync to repository** | `DAL/Interfaces/IFDCashFlowRepository.cs`, `DAL/Repositories/FDCashFlowRepository.cs` |

### Financial Logic Fixes

| # | Fix | Files Changed |
|---|-----|--------------|
| 11 | **Fixed accruedInterest double-counting** | `BAL/Services/FDInterestService.cs` — reset accruedInterest after non-compounding interest events |
| 12 | **Fixed maturity partial period** | `BAL/Services/FDInterestService.cs` — removed undocumented 1-day skip hack, simplified maturity interest logic |
| 13 | **Fixed FDCashFlowService.UpdateAsync recalculation** | `BAL/Services/FDCashFlowService.cs` — correct Days calculation, proper accruedInterest reset, correct interest amount per period |

### Frontend Fixes

| # | Fix | Files Changed |
|---|-----|--------------|
| 14 | **Fixed dashboard broken route** | `dashboard.component.html` — `/fd/new` → `/fd-detail` |
| 15 | **Added error UI feedback in FdInterestComponent** | `fd-interest.component.ts`, `fd-interest.component.html` — error message now displayed to user |

---

## 4. Files Modified Summary

### Backend (Created)
- `Backend/FinTrustFDManager.DAL/Repositories/DashboardRepository.cs`
- `Backend/FinTrustFDManager.BAL/Services/DashboardService.cs`

### Backend (Modified)
- `Backend/FinTrustFDManager.API/Program.cs`
- `Backend/FinTrustFDManager.API/appsettings.json`
- `Backend/FinTrustFDManager.API/Controllers/InvestmentController.cs`
- `Backend/FinTrustFDManager.API/Controllers/InvestmentApprovalController.cs`
- `Backend/FinTrustFDManager.API/Controllers/InterestFrequencyController.cs`
- `Backend/FinTrustFDManager.API/Controllers/DayCountConventionController.cs`
- `Backend/FinTrustFDManager.API/Controllers/CurrencyController.cs`
- `Backend/FinTrustFDManager.API/Controllers/CashFlowController.cs`
- `Backend/FinTrustFDManager.API/Controllers/BankController.cs`
- `Backend/FinTrustFDManager.API/Controllers/CounterPartyController.cs`
- `Backend/FinTrustFDManager.BAL/Common/DependencyInjection.cs`
- `Backend/FinTrustFDManager.BAL/Services/FDInterestService.cs`
- `Backend/FinTrustFDManager.BAL/Services/FDIdentificationService.cs`
- `Backend/FinTrustFDManager.BAL/Services/FDCashFlowService.cs`
- `Backend/FinTrustFDManager.DAL/Interfaces/IFDCashFlowRepository.cs`
- `Backend/FinTrustFDManager.DAL/Repositories/FDCashFlowRepository.cs`

### Frontend (Modified)
- `FinTrustFDManager.UI/src/app/core/services/entity.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/counter-party.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/counterparties.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/country.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/currency.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/interest-frequency.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/day-count-convention.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/fd-cash-flow.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/fd-interest.service.ts`
- `FinTrustFDManager.UI/src/app/core/services/dashboard.ts`
- `FinTrustFDManager.UI/src/app/core/services/bank.service.ts`
- `FinTrustFDManager.UI/src/app/services/auth.service.ts`
- `FinTrustFDManager.UI/src/app/services/counterparties.service.ts`
- `FinTrustFDManager.UI/src/app/services/country.service.ts`
- `FinTrustFDManager.UI/src/app/features/dashboard/dashboard.component.html`
- `FinTrustFDManager.UI/src/app/features/fd/fd-interest/fd-interest.component.ts`
- `FinTrustFDManager.UI/src/app/features/fd/fd-interest/fd-interest.component.html`

---

## 5. Remaining Issues (Not Yet Fixed)

### P1 — Still Open
- **FDCashFlowService.UpdateAsync not wrapped in transaction** — partial updates possible on failure
- **No global exception handling middleware** — unhandled exceptions return raw 500
- **N+1 queries in landing data** — `GetLandingDataAsync` fires 2 queries per FD

### P2 — Medium Priority
- **Duplicate auth services** — `services/auth.service.ts` (AuthService) vs `core/services/auth.ts` (Auth)
- **Duplicate FDLandingDto** — exists in both Model and BAL namespaces
- **No model validation on FDInterest/FDCashFlow endpoints** — accepts empty/invalid requests
- **Magic strings** — `"DRAFT"`, `"Interest"`, `"Maturity"` etc. used as raw strings everywhere
- **Frontend TypeScript `any` types** — `fd-detail.component.ts` uses `any` extensively
- **FDInterest frontend interface mismatch** — has fields not on backend (`calendarCode`, `tdsApplicable`, etc.)

### P3 — Nice to Have
- **No unit/integration tests**
- **No server-side pagination on FD listing**
- **No HTTPS/HSTS middleware**
- **Reference number generation race condition**
- **SessionStorage cache stale data risks**

---

## 6. Configuration Notes

### Environment Variables Required
```bash
# Database
DB_PASSWORD=<your_postgres_password>

# JWT
JWT_SECRET_KEY=<your_jwt_signing_key_at_least_32_chars>
```

### Frontend Environment
- `FinTrustFDManager.UI/src/environments/environment.ts` — `apiUrl: 'http://127.0.0.1:5075/api'`

### Backend Ports
- API: `http://localhost:5075` (HTTP), `https://localhost:7030` (HTTPS)
- Frontend: `http://localhost:4200`

---

## 7. Build Status

```
Backend:  BUILD SUCCEEDED (0 errors, 0 warnings)
Frontend: BUILD SUCCEEDED (213.70 kB initial bundle)
```
