# E-Commerce Microservices

A production-grade microservices system built with **.NET 8**, demonstrating real-world patterns including API Gateway, Saga, CQRS, Event Sourcing, Circuit Breaker, JWT Auth, Distributed Tracing, and Centralized Logging.

---

## Architecture Overview

```
Client (Web / Mobile)
        │
        ▼
┌─────────────────────┐
│    API Gateway       │  ← YARP + JWT Validation + Rate Limiting
│    localhost:5000    │
└─────────┬───────────┘
          │
    ┌─────┴──────┬───────────┬─────────────┬──────────────┐
    ▼            ▼           ▼             ▼              ▼
Identity      Product      Order        Payment       Inventory
:5001         :5002        :5003         :5004          :5005
    │            │           │             │              │
    ▼            ▼           ▼             ▼              ▼
 own DB       own DB      own DB        own DB         own DB
                              │
                              ▼
                        RabbitMQ (events)
                     ┌────────┴────────┐
                     ▼                 ▼
                 Payment           Notification
                 Service             :5006
                 :5004           (event consumer)
```

---

## Services

| Service | Port | Responsibility |
|---|---|---|
| API Gateway | 5000 | Routing, JWT validation, rate limiting, header injection |
| Identity | 5001 | Register, login, JWT issuance |
| Product | 5002 | Product catalogue, CRUD |
| Order | 5003 | Place orders, Saga orchestrator |
| Payment | 5004 | Process payments, idempotency |
| Inventory | 5005 | Stock management |
| Notification | 5006 | Email/SMS via event consumption |

---

## Patterns Implemented

| Pattern | Where |
|---------|-------|
| API Gateway | ECommerce.Gateway (YARP) |
| Database per Service | Each service has its own PostgreSQL database |
| Saga (Choreography) | Order → Payment → Inventory flow via RabbitMQ |
| CQRS | Order Service — separate read and write models |
| Event Sourcing | Order history stored as events |
| Circuit Breaker | Payment Service (Polly) |
| Retry with Backoff | All HTTP clients (Polly) |
| Bulkhead | Thread pool isolation per downstream service |
| JWT Authentication | Gateway validates once, injects trusted headers |
| Distributed Tracing | OpenTelemetry → Jaeger |
| Centralized Logging | Serilog → Seq |
| Eventual Consistency | Read models updated via RabbitMQ events |

---

## Technology Stack

| Concern | Library | Version |
|---------|---------|---------|
| Framework | .NET | 8.0 |
| API Gateway | YARP (Yet Another Reverse Proxy) | 2.x |
| ORM | Entity Framework Core | 8.0 |
| Database | PostgreSQL | 16 |
| Message Broker | RabbitMQ | 3.x |
| Async Messaging | MassTransit | 8.x |
| Resilience | Polly | 8.x |
| JWT Auth | Microsoft.AspNetCore.Authentication.JwtBearer | 8.0 |
| Password Hashing | BCrypt.Net-Next | 4.0 |
| Structured Logging | Serilog | 8.x |
| Log Viewer | Seq | latest |
| Distributed Tracing | OpenTelemetry | 1.x |
| Trace Viewer | Jaeger | latest |

---

## Prerequisites

Before running this project make sure you have:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

---

## Getting Started

### Step 1 — Clone the repository

```bash
git clone https://github.com/Dhruvil2105/E-commerce.git
cd E-commerce
```

### Step 2 — Start infrastructure (PostgreSQL, RabbitMQ, Seq, Jaeger)

```bash
docker-compose up -d
```

Wait about 30 seconds then verify all containers are running:

```bash
docker-compose ps
```

All 4 should show status **Up**:

```
ecommerce-postgres    Up
ecommerce-rabbitmq    Up
ecommerce-seq         Up
ecommerce-jaeger      Up
```

### Step 3 — Apply database migrations

```bash
cd src/Services/ECommerce.Identity
dotnet ef database update

cd ../ECommerce.Product
dotnet ef database update

cd ../ECommerce.Order
dotnet ef database update

cd ../ECommerce.Payment
dotnet ef database update

cd ../ECommerce.Inventory
dotnet ef database update
```

### Step 4 — Run all services

Open the solution in Visual Studio → right-click Solution → **Configure Startup Projects** → select **Multiple startup projects** → set all 7 projects to **Start**.

Or run each service individually:

```bash
cd src/Gateway/ECommerce.Gateway
dotnet run

cd src/Services/ECommerce.Identity
dotnet run
# repeat for all services
```

---

## Access URLs

| Service | URL |
|---|---|
| API Gateway | http://localhost:5000 |
| Identity Swagger | http://localhost:5001/swagger |
| Product Swagger | http://localhost:5002/swagger |
| Order Swagger | http://localhost:5003/swagger |
| Payment Swagger | http://localhost:5004/swagger |
| Inventory Swagger | http://localhost:5005/swagger |
| RabbitMQ Dashboard | http://localhost:15672 (guest / guest) |
| Seq Log Viewer | http://localhost:8081 (admin / admin123) |
| Jaeger Trace Viewer | http://localhost:16686 |

---

## API Endpoints

### Identity Service

```
POST /api/auth/register    Register a new user
POST /api/auth/login       Login and get JWT token
```

### Product Service

```
GET    /api/products           Get all products
GET    /api/products/{id}      Get product by ID
POST   /api/products           Create product (Admin only)
PUT    /api/products/{id}      Update product (Admin only)
DELETE /api/products/{id}      Delete product (Admin only)
```

### Order Service

```
POST   /api/orders             Place a new order
GET    /api/orders             Get my orders
GET    /api/orders/{id}        Get order by ID
```

### Inventory Service

```
GET    /api/inventory/{productId}    Get stock level
PUT    /api/inventory/{productId}    Update stock (Admin only)
```

---

## Happy Path Flow

```
1. POST /api/auth/register       → Create account
2. POST /api/auth/login          → Get JWT token
3. GET  /api/products            → Browse products
4. POST /api/orders              → Place order
          │
          ▼
   Order Service creates order (PENDING)
   Publishes → order.created event
          │
     ┌────┴────┐
     ▼         ▼
  Payment   Inventory
  charges   reserves
  customer  stock
     │         │
     └────┬────┘
          ▼
   Order confirmed (CONFIRMED)
   Publishes → order.confirmed event
          │
          ▼
   Notification Service
   sends confirmation email
```

## Failure Path Flow (Saga Compensation)

```
1. POST /api/orders              → Place order
2. Inventory reserves stock      ✓
3. Payment is declined           ✗ FAILED
          │
          ▼
   Compensating transactions:
   → Inventory releases reserved stock
   → Order marked CANCELLED
   → Notification sends cancellation email
```

---


## Key Design Decisions

### Why database per service?
Each service has its own PostgreSQL database and its own database user. The `identity_user` account literally cannot connect to `ecommerce_product`. Isolation is enforced at the infrastructure level — not just by coding convention.

### Why YARP for the gateway?
Microsoft-maintained, native ASP.NET Core integration, config-driven routing, and full support for custom middleware. Our JWT validation and header injection runs as standard ASP.NET Core middleware.

### Why MassTransit over raw RabbitMQ?
MassTransit handles consumer retries, dead-letter queues, saga state machines, and message deduplication out of the box. Raw RabbitMQ requires building all of this manually.

### Why record for events?
Events are immutable data — once published they never change. C# `record` types enforce value equality and immutability, which is exactly what event messages need.

---

## Environment Variables

Each service reads configuration from environment variables in production:

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | JWT issuer name |
| `RabbitMQ__Host` | RabbitMQ hostname |
| `RabbitMQ__Username` | RabbitMQ username |
| `RabbitMQ__Password` | RabbitMQ password |
