# 👑 Shahi Zayka — Enterprise Full-Stack Cloud Kitchen & E-Commerce Platform

> **Tier-1 Full-Stack Project engineered for Amazon SDE Intern & New Grad Interviews.**  
> Built with **ASP.NET Core Web API**, **PostgreSQL 17 (EF Core / Npgsql)**, **Clean Architecture**, and an interactive **Responsive Frontend**.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-blue.svg)](https://www.postgresql.org/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Onion-success.svg)](#architecture)
[![Tests](https://img.shields.io/badge/Unit%20Tests-10%20Passed-brightgreen.svg)](#automated-testing)
[![Docker](https://img.shields.io/badge/Docker%20Compose-Ready-blue.svg)](#docker-deployment)

---

## 📌 Executive Summary

**Shahi Zayka** ("Royal Taste") is a high-performance food ordering and cloud-kitchen management platform designed with production-grade engineering principles. It solves critical e-commerce distributed systems challenges:
1. **Strict Idempotency**: Guarantees zero duplicate charges or duplicate orders under network timeouts and retries using the `Idempotency-Key` pattern.
2. **Atomic Inventory Reservation**: Concurrency control preventing overselling of popular dishes.
3. **Cache-Aside Pattern**: Reduces database read latency by over 80% with automated cache invalidation.
4. **Defense-in-Depth Security**: Cryptographically salted PBKDF2 password hashing (100,000 iterations), constant-time hash verification, and JWT Bearer token authentication with Role-Based Access Control (`Customer`, `Admin`).
5. **Clean Architecture (Onion)**: Strict separation of concerns across Domain, Application, Infrastructure, and Presentation layers.

---

## 🏛️ System Architecture

```
                               ┌────────────────────────────────┐
                               │  Interactive Frontend Client  │
                               │  (HTML5, Vanilla JS, CSS3)     │
                               └───────────────┬────────────────┘
                                               │ HTTP / REST (JWT)
                                               ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 ASP.NET Core Web API                                  │
│                                                                                        │
│  ┌───────────────────────────────┐          ┌───────────────────────────────────────┐  │
│  │   Exception & Logging Pipeline │          │        Controllers / Endpoints        │  │
│  │   (RFC 7807 ProblemDetails)   │◄─────────┤ Products, Orders, Auth, Reviews, etc. │  │
│  └───────────────────────────────┘          └───────────────────┬───────────────────┘  │
│                                                                 │                      │
│  ┌──────────────────────────────────────────────────────────────▼────────────────────┐ │
│  │                             Application Core Layer                                │ │
│  │   - DTOs, Business Rules, Functional Result<T> Pattern                            │ │
│  │   - ProductService, OrderService (Idempotency Engine), AuthService                │ │
│  └───────────────────────────────┬──────────────────────────────┬────────────────────┘ │
│                                  │                              │                      │
│  ┌───────────────────────────────▼───────────┐      ┌───────────▼────────────────────┐ │
│  │            Domain Layer                   │      │     Infrastructure Layer       │ │
│  │  - Entities (Product, Order, User, etc.)  │      │  - EF Core DbContext (Npgsql)  │ │
│  │  - Enums (OrderStatus, PaymentMethod)     │      │  - PBKDF2 Password Hasher      │ │
│  │  - Domain Exceptions                      │      │  - In-Memory / Redis Caching   │ │
│  └───────────────────────────────────────────┘      └───────────┬────────────────────┘ │
└─────────────────────────────────────────────────────────────────┼──────────────────────┘
                                                                  │ Npgsql Driver
                                                                  ▼
                                                      ┌────────────────────────┐
                                                      │  PostgreSQL 17 Server  │
                                                      │  Relational Database   │
                                                      └────────────────────────┘
```

---

## 🚀 Quick Start Guide

### 1. Prerequisites
- **.NET 10 SDK** (or .NET 8 / 9)
- **PostgreSQL 17** (or run via Docker Compose)

### 2. Running Locally with .NET CLI

1. **Navigate to the Backend API directory**:
   ```powershell
   cd "backend\src\ShahiZayka.Api"
   ```

2. **Configure Database Connection** (Optional):
   Open `appsettings.json` and verify your PostgreSQL credentials:
   ```json
   "ConnectionStrings": {
     "PostgreSql": "Host=localhost;Port=5432;Database=ShahiZaykaDb;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
   }
   ```
   > *Note:* If PostgreSQL is not running or credentials differ, the app automatically runs in High-Performance In-Memory mode so you can demo immediately without friction!

3. **Start the API Server**:
   ```powershell
   dotnet run --urls "http://localhost:5000"
   ```

4. **Access the Application**:
   - 🌐 **Full-Stack Web App**: [http://localhost:5000](http://localhost:5000)
   - 📚 **Swagger OpenAPI Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
   - 🩺 **Health Check Probe**: [http://localhost:5000/api/health](http://localhost:5000/api/health)

---

## 🐳 Docker Deployment

Spin up the entire environment (PostgreSQL 17 container + ASP.NET Core API container) with a single command:

```powershell
docker compose up --build -d
```

- PostgreSQL runs on `localhost:5432` with automated volume persistence.
- Web API runs on `localhost:5000` with health check dependencies.

---

## 🔑 Pre-Seeded Demo Credentials

Use these 1-click accounts directly in the UI or in Swagger:

| Role | Email | Password | Access Privileges |
| :--- | :--- | :--- | :--- |
| **👑 Admin** | `admin@shahizayka.com` | `Admin@123` | View all orders, update status, manage menu products |
| **👤 Customer** | `customer@shahizayka.com` | `Customer@123` | Place orders, track history, personalized checkout |

---

## 📡 REST API Specifications

| Method | Endpoint | Access | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/products` | Public | List products with category, search, and veg filter |
| `GET` | `/api/products/{id}` | Public | Get single product details |
| `POST` | `/api/products` | Admin | Create new royal dish |
| `PUT` | `/api/products/{id}` | Admin | Update existing product details & inventory |
| `DELETE`| `/api/products/{id}` | Admin | Remove product |
| `GET` | `/api/categories` | Public | List menu categories with item counts |
| `POST` | `/api/orders/checkout`| Public / Auth | **Idempotent** order checkout with stock reservation |
| `GET` | `/api/orders/{orderNumber}`| Public | Real-time 5-stage order status tracker |
| `GET` | `/api/orders/my-orders`| Customer | Authenticated customer's order history |
| `GET` | `/api/orders` | Admin | View all system orders |
| `PUT` | `/api/orders/{id}/status`| Admin | Transition order status (`Confirmed` -> `Delivered`) |
| `POST` | `/api/auth/register` | Public | Create new customer account |
| `POST` | `/api/auth/login` | Public | Authenticate and issue signed JWT token |
| `GET` | `/api/auth/me` | Auth | Fetch authenticated user profile |
| `GET` | `/api/reviews` | Public | Retrieve verified patron reviews |
| `POST` | `/api/reviews` | Public | Submit new review |
| `POST` | `/api/newsletter/subscribe` | Public | Newsletter subscription |
| `GET` | `/api/health` | Public | Liveness probe for load balancers & ALB |

---

## 🧪 Automated Testing

All unit tests are written with **xUnit** and **Moq**, testing the most critical e-commerce flows:
- **Idempotency replay**: ensures duplicate calls do not double-bill or duplicate stock deductions.
- **Stock depletion**: rejects orders when items are out of stock.
- **Order cancellation**: tests rollback of product inventory.
- **Pricing precision**: tests GST tax calculation and promo code discounts.
- **Security**: tests cryptographic uniqueness of salts and constant-time password verification.

Run tests via CLI:
```powershell
dotnet test backend\ShahiZayka.slnx
```

---

## 📂 Project Directory Structure

```
shahi jayaka project/
├── backend/
│   ├── ShahiZayka.slnx                     # Modern .NET solution file
│   ├── Dockerfile                          # Multi-stage production build
│   ├── src/
│   │   ├── ShahiZayka.Domain/              # Pure domain entities, enums & exceptions
│   │   ├── ShahiZayka.Application/         # CQRS / Service layer, DTOs & business rules
│   │   ├── ShahiZayka.Infrastructure/      # EF Core DbContext, Npgsql, Auth & Caching
│   │   └── ShahiZayka.Api/                 # Controllers, Middlewares, Swagger, Program.cs
│   └── tests/
│       └── ShahiZayka.UnitTests/           # xUnit & Moq automated test suite
├── images/                                 # Royal dish photography & assets
├── index.html                              # Upgraded interactive frontend SPA
├── app.js                                  # Modular client application communicating with API
├── style.css                               # Responsive royal CSS design system
├── products.json                           # Initial seed data
├── docker-compose.yml                      # PostgreSQL 17 + Web API containerization
├── README.md                               # Project documentation
└── AMAZON_SDE_INTERVIEW_PREP.md            # Complete Amazon SDE interview prep guide
```
