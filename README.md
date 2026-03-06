# 🧠 DocSense: AI Document Intelligence API

> **Production-ready .NET 10 microservice** with AI integration, background job processing, and enterprise architecture patterns.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker)](https://www.docker.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## 🎯 Project Highlights

**Architecture & Design Patterns:**

- ✅ **Strategy Pattern** — AI provider abstraction (Gemini/OpenAI/Claude)
- ✅ **Factory Pattern** — Dynamic service instantiation
- ✅ **Repository Pattern** — Clean data access layer
- ✅ **Dependency Injection** — ASP.NET Core DI container
- ✅ **SOLID Principles** — Modular, testable, maintainable code

**Backend Engineering:**

- 🚀 **Background Job Processing** — Async task queue with retry logic
- 📊 **Database Versioning** — EF Core migrations with auto-apply on startup
- 🔒 **API Key Authentication** — Custom middleware implementation
- ⚡ **Performance** — Processes 1M+ characters in ~30 seconds
- 🐳 **Docker Compose** — Multi-container orchestration (API + PostgreSQL)

**Code Quality:**

- 🧪 **Unit Testing** — Moq, xUnit, in-memory SQLite
- 📦 **Clean Architecture** — Separated concerns (Controllers → Services → Repositories)
- 🔍 **Error Handling** — Custom exception filters & structured responses
- 📝 **OpenAPI Documentation** — Auto-generated Swagger UI

---

## 📘 Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
  - [Run with Docker](#run-with-docker)
  - [Local Development](#local-development)
- [Project Structure](#project-structure)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [Configuration](#configuration)
- [License](#license)

---

## ⚡ Features

**Document Processing:**

- 📄 Multi-format support: **PDF, DOCX, TXT**
- 📝 Text extraction via **PdfPig** (PDF), **OpenXML** (DOCX)
- ✂️ Smart text chunking for large documents (10M+ characters)

**AI Analysis:**

- 🤖 **4-in-1 Analysis**: Summary, Keywords, Sentiment, Main Topics
- 🔌 **Multi-provider support**: Gemini, OpenAI, Claude (pluggable architecture)
- ⚡ **Async processing**: Background jobs for long-running tasks

**Backend Features:**

- 🔐 **API Key Authentication** (middleware-based)
- 📊 **Usage Tracking** (quotas, limits per user plan)
- 💾 **PostgreSQL** for persistent storage
- 🐳 **Production-ready Docker setup**

---

## 🧰 Tech Stack

| Layer                | Technology                          |
| -------------------- | ----------------------------------- |
| **Language**         | C# 14.0                             |
| **Framework**        | ASP.NET Core 10                     |
| **Database**         | PostgreSQL 17 / SQLite (tests)      |
| **ORM**              | Entity Framework Core 10            |
| **AI Integration**   | Gemini API, OpenAI, Claude (custom) |
| **Testing**          | xUnit, Moq, FluentAssertions        |
| **Documentation**    | Swagger / OpenAPI 3.0               |
| **Containerization** | Docker, Docker Compose              |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    ASP.NET Core API                     │
├─────────────────────────────────────────────────────────┤
│  Controllers  →  Services  →  Repositories  →  Database │
│       ↓              ↓                                   │
│    DTOs        Strategies                               │
│                Factories                                 │
└─────────────────────────────────────────────────────────┘

Key Components:
├── ApiKeyMiddleware      → Authentication
├── PlanStrategy          → Usage quotas (Free/Pro/Ultra)
├── FileReaderStrategy    → Multi-format parsing
├── AiProviderStrategy    → AI abstraction layer
├── TextProcessingWorker  → Background job processor
└── JobQueue              → Async task management
```

**Design Patterns Used:**

- **Strategy** — Different AI providers, file readers, plan types
- **Factory** — Dynamic instantiation based on runtime config
- **Repository** — Data access abstraction
- **Middleware** — Authentication pipeline
- **Background Service** — Hosted service for job processing

---

## 🚀 Quick Start

### 🐳 Run with Docker

```bash
# 1) Clone the repository
git clone https://github.com/IakovYakovlev/DocSenseV1.git
cd DocSenseV1

# 2) Create .env file from example
cp DocSenseV1/.env.example DocSenseV1/.env

# 3) Add your AI API key to .env
# GeminiApiKey=your_key_here

# 4) Start services (API + PostgreSQL)
docker-compose up -d --build

# 5) Open Swagger UI
# http://localhost:5285/swagger
```

**Check logs:**

```bash
docker logs -f docsense-api
```

---

### 💻 Local Development

**Prerequisites:**

- .NET 10 SDK
- PostgreSQL 17
- API key for Gemini/OpenAI/Claude

**Steps:**

```bash
# 1) Restore dependencies
dotnet restore

# 2) Apply migrations
cd DocSenseV1
dotnet ef database update

# 3) Run API
dotnet run --project DocSenseV1

# 4) Run tests
dotnet test
```

---

## 📂 Project Structure

```
DocSenseV1/
├── Controllers/           # API endpoints
├── Services/              # Business logic
│   ├── AiProvider/        # AI integration (Strategy)
│   ├── FileReader/        # Document parsing (Strategy)
│   ├── Job/               # Background processing
│   ├── Plan/              # Usage quotas (Strategy)
│   ├── TextProcessing/    # Text chunking
│   └── Usage/             # Tracking & limits
├── Repositories/          # Data access (Repository)
├── Models/                # EF Core entities
├── Dtos/                  # Request/Response objects
├── Migrations/            # EF Core migrations
├── Authorization/         # API Key middleware
└── Program.cs             # Entry point + DI setup

DocSenseV1Test/
├── Services/              # Business logic tests
├── Repositories/          # Data access tests
├── Infrastructure/        # Middleware tests
└── Data/                  # Test helpers (in-memory DB)
```

---

## 🔌 API Endpoints

**Upload Document:**

```http
POST /api/upload
Headers:
  X-RapidAPI-Proxy-Secret: {your-api-key}
Body:
  file: sample.pdf
  user: userId
  planType: free|pro|ultra
```

**Check Job Result:**

```http
GET /api/job/{jobId}
Headers:
  X-RapidAPI-Proxy-Secret: {your-api-key}
```

📖 **Full API documentation:** `/swagger`

---

## 🧪 Testing

**Run all tests:**

```bash
dotnet test
```

**Run specific test category:**

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration
```

**Test coverage:**

- ✅ Repository layer (in-memory SQLite)
- ✅ Service layer (mocked dependencies)
- ✅ Strategy implementations
- ✅ Factory methods
- ✅ Middleware (exception handling)

---

## ⚙️ Configuration

**Environment Variables (`.env`):**

```env
# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080

# Database
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=docsensedbv1;Username=postgres;Password=root;SslMode=Disable

# AI Provider
AiProvider=Gemini
GeminiApiKey=your_key_here
GeminiModelName=gemini-2.5-flash-lite

# Authentication
Authentication__ApiKey=your_secret_api_key
```

**Supported AI Providers:**

- Gemini (Google)
- OpenAI (GPT-4)
- Claude (Anthropic)

---

## 📊 Technical Decisions

**Why Strategy Pattern?**

- Easy to add new AI providers without modifying existing code
- Each strategy encapsulates provider-specific logic
- Testable in isolation

**Why Background Jobs?**

- Large files (10M characters) take 30+ seconds to process
- Avoid HTTP timeouts
- Better UX (async status polling)

**Why PostgreSQL?**

- Production-ready RDBMS
- JSON support (for storing analysis results)
- EF Core migrations work seamlessly

**Why API Keys instead of JWT?**

- Simpler for API consumers
- No token refresh complexity
- Suitable for server-to-server communication

---

## 🔒 Security

- ✅ API keys stored securely (not in source control)
- ✅ File type validation (only PDF/DOCX/TXT)
- ✅ File size limits (configurable per plan)
- ✅ SQL injection protection (EF Core parameterized queries)
- ✅ Sensitive data not logged

---

## 📝 License

MIT © [Iakov Yakovlev](https://github.com/IakovYakovlev)

---

## 🤝 Contact

**GitHub:** [@IakovYakovlev](https://github.com/IakovYakovlev)  
**Project Link:** [DocSenseV1](https://github.com/IakovYakovlev/DocSenseV1)

---

**Built with ❤️ using .NET 10 and modern software engineering practices.**
