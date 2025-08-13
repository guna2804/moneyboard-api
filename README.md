# MoneyBoard Backend (ASP.NET Core 8 + PostgreSQL)
MoneyBoard is a Smart Personal Lending & Borrowing Tracker that helps individuals, families, and freelancers manage loans, repayments, interest calculations, and financial insights.  
This repository contains the backend API for MoneyBoard, built with ASP.NET Core 8, PostgreSQL, and Entity Framework Core.

🚀 Features
User Authentication & Authorization – JWT-based secure login/register.
Loan Management – Create, edit (with lock rules), and soft-delete loans.
Repayment Tracking – Log partial/full repayments, auto-allocation to interest first.
Interest Engine – Daily accrual, monthly compounding (MVP default).
Notifications – Upcoming and overdue repayment reminders (in-app + email ready).
Dashboard Aggregates – Role-based totals and repayment trends.
Audit Logging – Immutable history of changes for loans, repayments, and settings.
MVP-Ready – Lightweight, secure, and extendable architecture.

🛠 Tech Stack
Runtime: .NET 8 / ASP.NET Core Web API
Database: PostgreSQL
ORM: Entity Framework Core (Code-First Migrations)
Auth: JWT (Access & Refresh Tokens)
Background Jobs: Hangfire (scheduled interest posting, notifications)
Logging: Serilog
Validation: FluentValidation
Mapping: AutoMapper
Testing: xUnit, Moq

⚡ Getting Started
 1️⃣ Prerequisites
 .NET 8 SDK
 PostgreSQL (v14+)
 Docker (optional)
2️⃣ Setup
Clone the repository:
```bash
git clone https://github.com/your-org/moneyboard-api.git
cd moneyboard-api


