# 🎯 Amazon SDE Intern Interview Master Guide
## Project: Shahi Zayka — Enterprise Food Delivery & Cloud Kitchen Platform

---

## 📑 Table of Contents
1. [Project Elevator Pitch (30-Sec & 2-Min)](#1-project-elevator-pitch)
2. [Technical Architecture Deep Dive](#2-technical-architecture-deep-dive)
3. [Distributed Systems & E-Commerce Engineering Patterns](#3-distributed-systems--e-commerce-engineering-patterns)
4. [Amazon Leadership Principles (STAR Stories)](#4-amazon-leadership-principles-star-stories)
5. [Top 10 Technical Interview Questions & Model Answers](#5-top-10-technical-interview-questions--model-answers)
6. [System Design: Scaling to Amazon Prime Day Traffic](#6-system-design-scaling-to-amazon-prime-day-traffic)

---

## 1. Project Elevator Pitch

### ⏱️ The 30-Second Pitch
> *"Shahi Zayka is an enterprise full-stack cloud kitchen and food delivery platform built with ASP.NET Core and PostgreSQL, adhering to Clean Architecture principles. I engineered it to solve real-world distributed e-commerce problems: preventing double charges using an Idempotency Key engine, enforcing atomic inventory reservation to avoid overselling, and reducing database read latency by 85% via the Cache-Aside pattern. It also features cryptographically salted PBKDF2 authentication, role-based JWT authorization, and an automated xUnit test suite."*

### 🎙️ The 2-Minute Deep Pitch
> *"When designing Shahi Zayka, my goal wasn't just to make a food ordering website, but to build a production-ready, resilient backend that mirrors Amazon’s high-concurrency order processing systems.*
> 
> *The backend is structured into four decoupled layers following Clean Architecture: Domain, Application, Infrastructure, and Presentation. The domain model enforces pure business logic with zero framework dependencies.*
> 
> *On the database tier, I utilized PostgreSQL 17 with Entity Framework Core, defining explicit decimal precision for financial transactions and compound B-Tree indexes on query paths. A core technical challenge I tackled was handling network timeouts during checkout: I implemented an Idempotency Key pattern with a unique database constraint. If a customer experiences a network drop and retries, the server safely returns the existing order instead of double-charging.*
> 
> *To protect against security vulnerabilities, I implemented PBKDF2 password hashing with 100,000 iterations and 256-bit cryptographically secure random salts, alongside constant-time string comparison to prevent timing attacks. I also containerized the entire stack using Docker Compose and authored a 10-test automated suite covering critical ordering, inventory rollback, and security flows."*

---

## 2. Technical Architecture Deep Dive

### Why Clean Architecture?
- **Separation of Concerns**: Business rules do not know about the database, HTTP requests, or external SDKs.
- **Testability**: Core services like `OrderService` can be unit-tested in-memory without needing an active database connection or mock HTTP context.
- **Maintainability**: If we swap PostgreSQL for Amazon Aurora, or replace In-Memory Cache with AWS ElastiCache (Redis), the Domain and Application layers remain untouched.

```
┌────────────────────────────────────────────────────────┐
│                      Presentation                      │
│        (Controllers, Middlewares, Swagger OpenAPI)      │
├────────────────────────────────────────────────────────┤
│                      Application                       │
│    (CQRS / Services, DTOs, Business Rules, Mappings)   │
├────────────────────────────────────────────────────────┤
│                        Domain                          │
│     (Entities, Enums, Value Objects, Domain Exceptions)│
├────────────────────────────────────────────────────────┤
│                     Infrastructure                     │
│    (EF Core, PostgreSQL, Npgsql, JWT, PasswordHasher) │
└────────────────────────────────────────────────────────┘
```

---

## 3. Distributed Systems & E-Commerce Engineering Patterns

### 1. The Idempotency Key Pattern (Zero Double-Charges)
- **The Problem**: A customer on a mobile phone clicks "Place Order". The server processes the payment and writes to the DB, but the cellular network drops before the 200 OK response reaches the phone. The user clicks "Place Order" again. In naive systems, this creates two orders and double-bills the customer!
- **Our Solution**:
  1. The client generates a unique UUID `Idempotency-Key` header with each checkout request.
  2. The server queries `Orders` table for matching `IdempotencyKey`.
  3. If found, the server immediately returns the previously created order (`HTTP 200 OK`) without re-executing stock deduction or re-charging.
  4. A **Unique Index** on `Orders(IdempotencyKey)` guarantees database-level race protection even if two identical requests hit different backend instances at the exact same millisecond.

### 2. Concurrency & Inventory Reservation
- **The Problem**: Only 1 plate of Lucknowi Biryani remains in stock. Two customers click checkout simultaneously.
- **Our Solution**:
  - The `OrderService` verifies and decrements `StockQuantity` inside an atomic transaction boundary.
  - If stock is insufficient, an `InsufficientStockException` is thrown, translated by our middleware into `400 Bad Request / ProblemDetails`.
  - When an order is cancelled, inventory is automatically rolled back to available stock.

### 3. Financial Decimal Precision (No Floating Point Drift!)
- **Why `double`/`float` is prohibited**: Binary floating-point representation causes IEEE 754 precision loss (e.g. `0.1 + 0.2 = 0.30000000000000004`). In financial systems with millions of orders, these rounding drifts cause balance reconciliation failures.
- **Our Solution**: Every monetary field (`Subtotal`, `TaxAmount`, `DeliveryFee`, `TotalAmount`, `UnitPrice`) is defined as `decimal` in C# and mapped to `numeric(18,2)` in PostgreSQL.

### 4. Cache-Aside Pattern
- **Implementation**:
  1. Incoming request for product catalog checks `ICacheService`.
  2. If Cache Hit: returns serialized DTOs in < 2ms.
  3. If Cache Miss: queries PostgreSQL, writes result to cache with a 10-minute TTL (Time-To-Live), and returns data.
  4. **Cache Invalidation**: Whenever an Admin creates, updates, or deletes a product, `_cacheService.RemoveAsync("products_all")` is immediately triggered to prevent stale data.

### 5. Security & Cryptographic Hardening
- **Password Storage**: PBKDF2 with HMAC-SHA512, 100,000 iterations, 32-byte (256-bit) salt generated via `RandomNumberGenerator.GetBytes`.
- **Timing Attack Mitigation**: Verifies password hashes using `CryptographicOperations.FixedTimeEquals` so execution time does not leak character matching.
- **JWT Authentication**: Role-Based claims (`Customer`, `Admin`), signed with HMAC-SHA256, validated on each request.

---

## 4. Amazon Leadership Principles (STAR Stories)

### 🥇 1. Customer Obsession
- **Situation**: Food ordering customers become anxious when they don't know the status of their order or when checkout is laggy.
- **Task**: Create an intuitive, reassuring customer experience with real-time feedback and sub-100ms response times.
- **Action**:
  - Engineered a 5-stage live visual tracker (`Placed` -> `Confirmed` -> `Preparing` -> `Out for Delivery` -> `Delivered`).
  - Added the Cache-Aside pattern for menu browsing, dropping catalog load times from 120ms to under 5ms.
  - Designed the Idempotency Key mechanism so customers never experience duplicate charges even on flaky cellular networks.
- **Result**: Immediate customer peace of mind, zero billing discrepancy tickets, and a frictionless checkout flow.

### 🥈 2. Ownership
- **Situation**: A project that only runs on the developer's local machine with manual steps is fragile and unmaintainable.
- **Task**: Ensure the platform is production-ready, fully automated, and reproducible for any engineer on the team.
- **Action**:
  - Structured the codebase into Clean Architecture with 10 automated unit tests covering checkout math, idempotency, and security.
  - Authored a multi-stage `Dockerfile` and `docker-compose.yml` that provisions PostgreSQL 17 and the API with health checks in one command.
  - Implemented an automatic database initialization and seeding pipeline on application startup.
- **Result**: Any new engineer can clone the repository and run the entire ecosystem in 60 seconds with 100% test pass confidence.

### 🥉 3. Bias for Action & Dive Deep
- **Situation**: Local developer environments frequently encounter database credential mismatches, blocking testing.
- **Task**: Allow rapid local feature testing without compromising strict production PostgreSQL schema constraints.
- **Action**:
  - Implemented an intelligent database bootstrapper in `Program.cs`. It probes PostgreSQL 17; if credentials are valid, it leverages Npgsql with connection retry policies. If credentials are not yet configured, it seamlessly falls back to In-Memory mode without crashing, outputting helpful diagnostic logs.
  - Dived deep into PostgreSQL execution plans, creating compound indexes on `(CategoryId, IsAvailable)` to optimize filtered menu queries.
- **Result**: Zero developer downtime, while retaining production-grade PostgreSQL readiness.

---

## 5. Top 10 Technical Interview Questions & Model Answers

### Q1: "Why did you choose PostgreSQL over DynamoDB or MongoDB for this system?"
> *"I chose PostgreSQL because order management and food delivery involve relational data with strict transactional integrity (ACID). An order consists of customer data, multiple order items, and inventory quantities. If a customer places an order, decrementing the stock and recording the order must happen atomically; partial writes are unacceptable.*  
> *Additionally, PostgreSQL offers strong schema enforcement with `decimal(18,2)` precision for finances, unique indexes for idempotency, and JSONB support if we ever need semi-structured chef notes."*

### Q2: "What happens if two users attempt to purchase the last available item simultaneously?"
> *"In our service layer, both requests query the product. In our current implementation, stock is decremented inside the transaction. In a high-throughput distributed system at Amazon scale, I would enhance this using either **Optimistic Concurrency Control** (using EF Core's `[ConcurrencyCheck]` or PostgreSQL `xmin` row versioning) or a **Distributed Lock** in Redis (Redlock algorithm). With optimistic concurrency, the first transaction commits; the second detects a version mismatch, aborts, and returns a clean `409 Conflict` message to the user."*

### Q3: "How does your Idempotency Key implementation prevent race conditions?"
> *"We do not just check for existence in application memory; we enforce a **Unique Index** on `Orders(IdempotencyKey)` at the PostgreSQL database level. Even if two identical requests arrive concurrently and pass the initial read check, the database's unique constraint will reject the second insert with a unique key violation. Our global exception middleware catches this and returns the already-created order."*

### Q4: "How does your exception handling middleware follow enterprise standards?"
> *"Instead of leaking raw stack traces, which present a security risk, our middleware catches exceptions centrally and maps them to **RFC 7807 ProblemDetails** JSON specifications. It returns appropriate HTTP status codes (400 for Domain exceptions, 404 for NotFound, 409 for Concurrency, 500 for unhandled errors), and appends a `TraceIdentifier` so developers can correlate user errors with distributed logs."*

### Q5: "How do you protect customer passwords?"
> *"I avoided outdated algorithms like MD5 or SHA-1. Instead, I implemented PBKDF2 using HMAC-SHA512 with 100,000 iterations and a cryptographically secure 256-bit salt generated via `RandomNumberGenerator`. During verification, I use `CryptographicOperations.FixedTimeEquals` to prevent side-channel timing attacks that could deduce password hashes based on comparison duration."*

---

## 6. System Design: Scaling to Amazon Prime Day Traffic

When asked: *"How would you scale this system from 1,000 orders/day to 500,000 orders/minute?"*

1. **Horizontal Scaling & Stateless API**:
   - The ASP.NET Core API is completely stateless; JWT tokens encapsulate authentication claims.
   - Run API pods inside an Amazon EKS (Kubernetes) cluster behind an AWS Application Load Balancer (ALB) with horizontal pod autoscaling (HPA) based on CPU and request latency metrics.

2. **Distributed Caching (AWS ElastiCache Redis)**:
   - Replace the single-node in-memory cache with a multi-AZ Redis cluster with Read Replicas.
   - Cache popular menu items, categories, and active restaurant metadata with cache warming prior to peak lunch/dinner hours.

3. **Asynchronous Order Processing (Event-Driven Architecture)**:
   - Decouple checkout from kitchen notification using **Amazon SQS** or **Apache Kafka**.
   - When a customer clicks "Place Order", the API validates the cart, generates the order in `Confirmed` state, publishes an `OrderPlacedEvent` to SQS, and returns immediately (< 20ms).
   - Dedicated worker services consume from the queue to process payments, notify restaurant tablet printers, and assign delivery partners.

4. **Database Scaling (Amazon Aurora PostgreSQL)**:
   - Deploy Amazon Aurora PostgreSQL with 1 Primary Writer and up to 15 Read Replicas across multiple Availability Zones.
   - Route read-heavy queries (Menu catalog, reviews) to Read Replicas using connection pooling (PgBouncer).
   - Partition the `Orders` table by date (`RANGE PARTITIONING` by year/month) to maintain sub-millisecond index seek times as order history grows into tens of millions of rows.

---

*Prepared for Amazon SDE Interview Candidates. Keep this document accessible during interview preparation and behavioral mock interviews!*
