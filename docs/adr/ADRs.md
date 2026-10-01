# ADR-001: Use PostgreSQL as Primary Database

## Status
Accepted

## Context
We need a primary data store for the BackOps Simulator that supports:
- ACID transactions for payment processing
- Complex relational data (events, tickets, purchases, payments)
- JSON support for flexible payloads
- Free tier availability for demo deployment
- Good .NET/EF Core support

## Decision
Use PostgreSQL 16 as the primary database.

## Alternatives Considered
1. **SQL Server** - Excellent .NET integration but limited free tier options
2. **MySQL** - Good free tier but weaker JSON support and transaction handling
3. **SQLite** - Zero config but not suitable for concurrent workloads
4. **Cosmos DB** - Good free tier but NoSQL model doesn't fit relational domain

## Consequences
### Positive
- Full ACID compliance for financial transactions
- Rich JSON support for webhook payloads and flexible data
- Supabase offers generous free tier (500MB, 1GB storage, 5GB egress)
- Excellent EF Core provider with good performance
- Mature ecosystem and tooling

### Negative
- Connection pooling required for serverless functions
- Free tier projects pause after 1 week of inactivity
- Schema migrations required for schema changes

## Implementation
- Entity Framework Core 9 with Npgsql provider
- Code-first migrations
- Connection pooling configured
- Retry policy for transient failures

---

# ADR-002: Use Redis for Queue and Caching

## Status
Accepted

## Context
We need a message queue and cache layer that supports:
- High-throughput message queuing for async processing
- Consumer groups for work distribution
- Idempotency key storage with TTL
- Rate limiting counters
- Free tier availability

## Decision
Use Redis 7 with Redis Streams for queuing and caching.

## Alternatives Considered
1. **Apache Kafka** - Excellent for high throughput but complex ops, no free tier
2. **RabbitMQ** - Good features but heavier, limited free tier
3. **Azure Service Bus** - Good but cloud-specific, costs at scale
4. **In-memory** - Not suitable for distributed workers

## Consequences
### Positive
- Redis Streams provides lightweight queue with consumer groups
- Upstash offers generous free tier (256MB, 500K commands/month, 10GB bandwidth)
- Sub-millisecond latency for caching
- Built-in TTL support for idempotency keys
- Pub/Sub for real-time updates

### Negative
- Redis Streams less mature than Kafka for complex routing
- Memory-limited (not suitable for massive event retention)
- Free tier has command limits

## Implementation
- StackExchange.Redis client
- Custom IMessageBus abstraction (Redis/Kafka swapable)
- Redis Streams for queue with consumer groups
- Redis Cache for idempotency and rate limiting

---

# ADR-003: Idempotency Strategy with Redis

## Status
Accepted

## Context
Payment and purchase operations must be idempotent to prevent:
- Double charges from network retries
- Duplicate ticket reservations
- Webhook reprocessing

## Decision
Implement idempotency using Redis with 24-hour TTL keys.

## Alternatives Considered
1. **Database unique constraint** - Works but requires DB round-trip, no TTL
2. **In-memory dictionary** - Not distributed, lost on restart
3. **Distributed lock** - Adds latency, complex to implement correctly

## Consequences
### Positive
- Sub-millisecond check/set operations
- Automatic expiration prevents memory bloat
- Works across multiple API instances
- Simple implementation with Redis SETNX

### Negative
- Redis dependency for critical path
- 24-hour window may not suit all use cases
- Clock skew between services

## Implementation
- `IIdempotencyService` abstraction with `RedisIdempotencyService`
- Key format: `idempotency:{key}`
- Value: Entity ID (GUID) for tracking
- TTL: 24 hours
- Returns existing entity ID on collision

---

# ADR-004: Retry Policy with Exponential Backoff

## Status
Accepted

## Context
External dependencies (bank simulator, webhooks) can fail transiently. We need a consistent retry strategy.

## Decision
Use Polly with exponential backoff, jitter, and configurable max retries.

## Alternatives Considered
1. **Fixed interval** - Doesn't handle thundering herd
2. **Linear backoff** - Better but still causes synchronization
3. **No retry** - Poor resilience

## Consequences
### Positive
- Jitter prevents thundering herd
- Configurable per dependency
- Integrates with circuit breaker
- Observable via metrics

### Negative
- Adds latency on failure paths
- Can mask underlying issues if not monitored
- Requires idempotency for safety

## Implementation
- `IRetryPolicy` abstraction with `RetryPolicy` implementation
- Default: 3 retries, 100ms base, 2x multiplier, 100ms jitter
- Retryable exceptions: HttpRequestException, TimeoutException, TaskCanceledException
- Metrics emitted for each retry attempt

---

# ADR-005: Circuit Breaker for External Dependencies

## Status
Accepted

## Context
Bank simulator and webhook endpoints can become unavailable. Continuous calls waste resources and cascade failures.

## Decision
Implement circuit breaker pattern for external HTTP calls.

## Alternatives Considered
1. **Timeout only** - Doesn't prevent repeated attempts
2. **Bulkhead only** - Limits concurrency but doesn't stop calls
3. **External service mesh** - Overkill for this scope

## Consequences
### Positive
- Fast failure when dependency is down
- Automatic recovery via half-open state
- Prevents cascade failures
- Observable state changes

### Negative
- Added complexity
- Requires tuning (threshold, timeout)
- Can cause false positives

## Implementation
- `ICircuitBreaker` abstraction with `CircuitBreaker` implementation
- Default: 5 failures opens, 30s timeout, 2 successes closes
- Events for state changes
- Applied to bank simulator and webhook delivery

---

# ADR-006: Observability Stack - OpenTelemetry + Grafana

## Status
Accepted

## Context
Need vendor-neutral observability for metrics, traces, and logs.

## Decision
Use OpenTelemetry for instrumentation with Jaeger (traces), Prometheus (metrics), Grafana (dashboards), Seq (logs).

## Alternatives Considered
1. **Application Insights** - Azure-specific, costs at scale
2. **Datadog** - Expensive, vendor lock-in
3. **Elastic Stack** - Heavy, complex setup
3. **Custom** - Reinventing the wheel

## Consequences
### Positive
- Vendor-neutral instrumentation
- Industry standard (CNCF)
- Free tier available for all components
- Rich ecosystem of exporters
- Works locally and in cloud

### Negative
- Multiple components to operate
- Learning curve for OpenTelemetry
- Resource overhead

## Implementation
- OpenTelemetry SDK in ASP.NET Core
- OTLP exporter to Jaeger/Prometheus
- Auto-instrumentation for ASP.NET Core, HttpClient, EF Core, Redis
- Custom metrics for business KPIs
- Structured logging with Serilog → Seq

---

# ADR-007: Modular Monolith First

## Status
Accepted

## Context
Microservices add significant complexity. We need to balance learning value with maintainability.

## Decision
Start with a modular monolith. Extract services only when justified.

## Alternatives Considered
1. **Microservices from start** - High complexity, operational burden
2. **Single project monolith** - No clear boundaries, hard to extract later
3. **Serverless functions** - Cold starts, vendor lock-in, debugging difficulty

## Consequences
### Positive
- Clear module boundaries (Domain, Application, Infrastructure)
- Easy to extract services later
- Single deployment unit
- Simpler debugging and testing
- Lower operational overhead

### Negative
- Single point of failure
- Technology lock-in within modules
- Scaling is all-or-nothing
- Team autonomy limited

## Implementation
- Solution folders for each module
- Internal nuget packages not used (project references)
- Clear dependency direction: Domain ← Application ← Infrastructure
- MediatR for in-process messaging
- Ready for future extraction

---

# ADR-008: Kafka as Advanced Lab Option

## Status
Proposed

## Context
Redis Streams is sufficient for free tier but Kafka is industry standard for event streaming.

## Decision
Make Kafka available via Docker for advanced labs, but default to Redis Streams.

## Alternatives Considered
1. **Kafka only** - No free tier, complex ops
2. **Redis only** - Limits learning for enterprise patterns
3. **Both always** - Resource overhead

## Consequences
### Positive
- Students can compare Redis vs Kafka
- Kafka available for advanced scenarios
- Free tier stays lightweight
- Demonstrates abstraction pattern

### Negative
- Two queue implementations to maintain
- Feature parity challenges
- Additional Docker resources

## Implementation
- `IQueueProvider` abstraction with `RedisMessageBus` and `KafkaMessageBus`
- Profile-based Docker Compose (`--profile distributed`)
- Kafka only in local/advanced mode
- Same consumer group semantics