# FinTrust FD Manager - Project Context

## Overview
**Project Name**: FinTrust FD Manager (FDManagementSystem)
**Description**: A full-stack web application designed to manage Fixed Deposits (FDs). It handles FD creation, interest rate calculations (simple and compounding), cash flow generation, and maturity tracking.

## Technology Stack
- **Backend**: .NET 8 (C#) Web API
- **Frontend**: Angular (TypeScript)
- **Database ORM**: Entity Framework Core

## Architecture

### Backend (`/Backend/`)
The backend is structured into an N-Tier architecture:
1. **FinTrustFDManager.API**: The presentation layer containing Controllers (e.g., `FDIdentificationController`, `FDInterestController`, `FDCashFlowController`). It handles HTTP routing and exposes RESTful endpoints.
2. **FinTrustFDManager.BAL (Business Access Layer)**: Contains the core business logic and services (e.g., `FDInterestService.cs`, `FDIdentificationService.cs`). Handles calculations, validation, and orchestrating data flow.
3. **FinTrustFDManager.DAL (Data Access Layer)**: Contains the EF Core `DbContext`, Migrations, and Repositories. Interfaces directly with the database.
4. **FinTrustFDManager.Model**: Contains the core Entities (e.g., `FixedDeposit`, `FDInterest`, `FDCashFlow`) and DTOs used to transfer data between layers.

### Frontend (`/Frontend/FinTrustFDManager.UI/`)
The frontend is built with Angular.
- Uses **RxJS** for reactive programming and state/API management (e.g., `switchMap`, `forkJoin`, `finalize`).
- Organized by features under `src/app/features/` (e.g., `fd/`, `dashboard/`, `entities/`).
- Components include `FDDetailComponent`, `FDListComponent`, `FDCashflowComponent`, etc.
- Uses standard Angular services to communicate with the .NET backend.

## Core Domain Logic
- **FD Identification**: The general details of a Fixed Deposit (Principal, Start/End dates, Entity, Counterparty).
- **Interest Settings**: Frequency (Monthly, Quarterly, Annually, At Maturity), Compounding settings, Interest Rates, and Calculation Basis (e.g., Actual/365).
- **Cash Flow Generation**: Based on the FD Identification and Interest Settings, the system generates a schedule of cash flows representing Interest Accruals, Compounding Events, and Final Maturity payouts.

## Key Developer Notes
- The backend `FDInterestService.cs` contains complex financial logic for generating compounding cash flows accurately without double-counting interest.
- The frontend relies on reactive RxJS pipelines to handle route changes (e.g., navigating between different FDs rapidly) safely without race conditions.
