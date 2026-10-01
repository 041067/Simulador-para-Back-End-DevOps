# BackOps Simulator

[![Build Status](https://github.com/041067/Simulador-para-Back-End-DevOps/workflows/CI/badge.svg)](https://github.com/041067/Simulador-para-Back-End-DevOps/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[!.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)
[!React 18](https://img.shields.io/badge/React-18-blue.svg)
[!Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)

A hands-on laboratory for distributed systems, backend engineering, DevOps and SRE.

## 🎯 Overview

BackOps Simulator is an educational platform that simulates real-world distributed systems scenarios under load, concurrency, and failure conditions. It's designed for developers at all levels to learn and experiment with:

- **High Load & Concurrency** - Ticket sales with overselling prevention
- **Queue Processing** - Video streaming with backpressure handling
- **Payment Gateway** - Idempotency, retries, circuit breakers, webhooks
- **Chaos Engineering** - Controlled failure injection
- **Observability** - Metrics, distributed tracing, structured logging

## 🏗️ Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Frontend  │────▶│  API GW     │────▶│  Workers    │
│  React/TS   │     │  ASP.NET    │     │  Background │
└─────────────┘     └──────┬──────┘     └──────┬──────┘
                           │                   │
                    ┌──────┴──────┐     ┌──────┴──────┐
                    │  PostgreSQL │     │    Redis    │
                    │  (Supabase) │     │  (Upstash)  │
                    └─────────────┘     └─────────────┘
                           │                   │
                    ┌──────┴──────┐     ┌──────┴──────┐
                    │  Jaeger     │     │  Grafana    │
                    │  (Tracing)  │     │  (Metrics)  │
                    └─────────────┘     └─────────────┘
```

## 🚀 Quick Start

### Prerequisites
- Docker Desktop 4.0+
- .NET 9 SDK
- Node.js 20+
- Git

### Local Development

```bash
# 1. Clone the repository
git clone https://github.com/041067/Simulador-para-Back-End-DevOps.git
cd Simulador-para-Back-End-DevOps

# 2. Start infrastructure
docker compose up -d postgres redis jaeger seq

# 3. Run database migrations
dotnet ef database update --project src/BackOps.Infrastructure --startup-project apps/api

# 4. Start the API
dotnet run --project apps/api

# 5. Start the Worker (in another terminal)
dotnet run --project apps/worker

# 6. Start the Frontend (in another terminal)
cd apps/web
npm install
npm run dev
```

### Access Points
- **Frontend**: http://localhost:5173
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health
- **Jaeger**: http://localhost:16686
- **Seq**: http://localhost:5341

## 🧪 Labs

### 1. High Load Lab (`/high-load`)
Simulate ticket sales under extreme concurrency:
- Configure users (100 - 50,000)
- Requests per user
- Worker pool size
- Queue provider (Redis/Kafka)
- Failure injection

**Key Concepts**: Race conditions, optimistic/pessimistic locking, queue-based processing, backpressure

### 2. Streaming Lab (`/streaming`)
Video processing pipeline with backpressure:
- Thousands of concurrent video jobs
- Priority queue processing
- Worker scaling
- Throughput vs latency tradeoffs

**Key Concepts**: Little's Law, consumer groups, priority queues, resource saturation

### 3. Payment Gateway Lab (`/payments`)
Production-grade payment processing:
- Idempotency keys (prevent double charges)
- Retry with exponential backoff
- Circuit breaker for bank simulator
- Webhook delivery with retry
- Rate limiting

**Key Concepts**: Idempotency, state machines, distributed transactions, compensation

### 4. Chaos Engineering (`/chaos`)
Controlled failure injection:
- Database latency/unavailability
- Redis failures
- Worker crashes
- Network timeouts
- Random errors

**Key Concepts**: Failure domains, blast radius, recovery procedures, MTTR

## 📊 Observability

### Metrics (Prometheus/Grafana)
- Request rate, latency (p50/p95/p99), error rate
- Queue depth, processing time, throughput
- Worker utilization, job success/failure rates
- Circuit breaker state, retry counts
- Payment success/failure rates

### Distributed Tracing (Jaeger)
- End-to-end request flow
- Service dependencies
- Bottleneck identification
- Error propagation

### Structured Logging (Seq)
- Correlation IDs across services
- Structured event data
- Real-time log streaming

## 🔧 Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 18, TypeScript, Vite, Tailwind CSS, TanStack Query, Recharts |
| API | ASP.NET Core 9, Minimal APIs, MediatR, FluentValidation |
| Workers | .NET 9 Background Services |
| Database | PostgreSQL 16, Entity Framework Core 9 |
| Cache/Queue | Redis 7 (Streams), StackExchange.Redis |
| Messaging | Custom IMessageBus abstraction (Redis/Kafka) |
| Resilience | Polly (Retry, Circuit Breaker), Custom Rate Limiter, Idempotency |
| Observability | OpenTelemetry, Jaeger, Prometheus, Grafana, Seq, Serilog |
| Testing | xUnit, Moq, FluentAssertions, Testcontainers, k6 |
| CI/CD | GitHub Actions, Docker, Vercel, Render |

## 📁 Project Structure

```
backops-simulator/
├── apps/
│   ├── web/                 # React frontend
│   ├── api/                 # ASP.NET Core Web API
│   └── worker/              # Background workers
├── src/
│   ├── BackOps.Domain/      # Domain layer (pure)
│   ├── BackOps.Application/ # Application layer (CQRS)
│   ├── BackOps.Infrastructure/ # Infrastructure (EF, Redis, etc.)
│   └── BackOps.Contracts/   # Shared contracts
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   ├── ArchitectureTests/
│   └── ContractTests/
├── load-tests/
│   └── k6/                  # k6 load test scripts
├── infrastructure/
│   ├── docker/              # Docker files
│   ├── grafana/             # Grafana dashboards
│   └── terraform/           # IaC (future)
├── docs/
│   ├── architecture/        # Architecture docs
│   ├── scenarios/           # Scenario guides
│   ├── adr/                 # Architecture Decision Records
│   └── runbooks/            # Operational runbooks
└── docker-compose.yml
```

## 🧪 Testing

```bash
# Run all tests
dotnet test BackOpsSimulator.sln

# Run specific test projects
dotnet test tests/UnitTests
dotnet test tests/IntegrationTests
dotnet test tests/ArchitectureTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run k6 load tests
k6 run load-tests/k6/baseline.js
k6 run load-tests/k6/stress.js
k6 run load-tests/k6/spike.js
k6 run load-tests/k6/soak.js
k6 run load-tests/k6/payment.js
```

## 🚀 Deployment

### Free Tier Production
- **Frontend**: Vercel (Hobby)
- **API**: Render (Free Web Service)
- **Database**: Supabase (Free PostgreSQL)
- **Redis**: Upstash (Free Redis)
- **Observability**: Grafana Cloud (Free)

### CI/CD Pipeline
GitHub Actions handles:
1. Build & test on every PR
2. Security scanning (Trivy)
3. Docker image build & push
4. Deploy to staging on merge to main
5. Production deploy on tag

## 📚 Documentation

- [Architecture Overview](docs/architecture/README.md)
- [Scenario Guides](docs/scenarios/)
- [ADRs](docs/adr/)
- [Runbooks](docs/runbooks/)
- [API Reference](http://localhost:8080/swagger)

## 🎓 Learning Paths

### Junior Developer
- Run simulations and observe behavior
- Understand API contracts
- Learn basic queue patterns
- Practice with Docker

### Mid-level Developer
- Modify concurrency settings
- Implement custom retry policies
- Work with Redis Streams
- Analyze metrics and traces

### Senior Developer
- Design alternative architectures
- Optimize throughput/latency
- Implement custom circuit breakers
- Conduct chaos experiments
- Evaluate trade-offs (CAP, consistency)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Run the full test suite
6. Submit a PR

See [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🙏 Acknowledgments

- Inspired by real-world distributed systems challenges
- Built with modern .NET and React ecosystems
- Designed for learning, not production use

---

**⚠️ Disclaimer**: This is an educational simulator. No real financial transactions are processed. The free tier deployment is for demonstration only and does not provide production-grade HA/SLA guarantees.