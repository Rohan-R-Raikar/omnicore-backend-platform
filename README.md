# OmniCore – Enterprise Backend Platform

OmniCore is a production-style backend platform built using **ASP.NET Core** and **Clean Architecture** principles.  
The project demonstrates how to design scalable, secure, and maintainable backend systems similar to those used in modern enterprise environments.

## 🚀 Features

- Clean Architecture (Domain, Application, Infrastructure, API)
- ASP.NET Core Web API
- Entity Framework Core for data access
- JWT & OpenID Connect authentication
- Real-time communication using SignalR
- Background job processing using Hangfire
- Structured logging with Serilog

## 🏗 Architecture
```
OmniCore
│
├── OmniCore.API → Presentation Layer (Controllers, Middleware)
├── OmniCore.Application → Business Logic
├── OmniCore.Domain → Core Entities & Domain Rules
├── OmniCore.Infrastructure→ External Services
└── OmniCore.Persistence → Database Access (EF Core)
```

This structure follows **Clean Architecture**, ensuring separation of concerns and maintainability.

## ⚙️ Tech Stack

- ASP.NET Core
- Entity Framework Core
- SignalR
- Hangfire
- Serilog
- JWT Authentication
- OpenID Connect
- Docker (planned)

## 📌 Project Goals

The goal of OmniCore is to build a **production-like backend system** demonstrating best practices in:

- API design
- backend architecture
- scalability
- maintainability
- security

## 🛠 Development Status

🚧 Currently under active development.

Core modules being implemented:
- Authentication system
- Background job processing
- Real-time notification service
- Logging & monitoring

## 👨‍💻 Author

**Rohan Raikar**

Backend Developer focused on **ASP.NET Core, scalable APIs, and enterprise backend architecture**.