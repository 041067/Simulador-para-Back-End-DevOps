import { useState } from 'react';
import { FileText, BookOpen, Code, Terminal, Zap, Shield, GitBranch, ExternalLink, ChevronDown, ChevronRight, Search, Copy } from 'lucide-react';
import { clsx } from 'clsx';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/Tabs';

const docsSections = [
  {
    id: 'getting-started',
    title: 'Getting Started',
    icon: BookOpen,
    children: [
      { id: 'quickstart', title: 'Quick Start', description: 'Run the simulator locally in 5 minutes' },
      { id: 'prerequisites', title: 'Prerequisites', description: 'Required tools and accounts' },
      { id: 'configuration', title: 'Configuration', description: 'Environment variables and settings' },
      { id: 'docker', title: 'Docker Compose', description: 'Local development with Docker' },
    ],
  },
  {
    id: 'labs',
    title: 'Labs',
    icon: Zap,
    children: [
      { id: 'high-load', title: 'High Load Lab', description: 'Ticket sales under extreme concurrency' },
      { id: 'streaming', title: 'Streaming Lab', description: 'Video processing with backpressure' },
      { id: 'payments', title: 'Payment Gateway Lab', description: 'Idempotency, retries, circuit breakers' },
      { id: 'chaos', title: 'Chaos Engineering', description: 'Failure injection and resilience testing' },
    ],
  },
  {
    id: 'architecture',
    title: 'Architecture',
    icon: Code,
    children: [
      { id: 'overview', title: 'System Overview', description: 'High-level architecture diagram' },
      { id: 'domain-model', title: 'Domain Model', description: 'Entities, value objects, and aggregates' },
      { id: 'data-flow', title: 'Data Flow', description: 'Request lifecycle and event flow' },
      { id: 'deployment', title: 'Deployment Architecture', description: 'Production and local setups' },
    ],
  },
  {
    id: 'guides',
    title: 'Guides',
    icon: Terminal,
    children: [
      { id: 'concurrency', title: 'Concurrency Patterns', description: 'Optimistic locking, pessimistic locking, queues' },
      { id: 'idempotency', title: 'Implementing Idempotency', description: 'Keys, storage, and best practices' },
      { id: 'circuit-breaker', title: 'Circuit Breaker Pattern', description: 'Configuration and tuning' },
      { id: 'observability', title: 'Observability Setup', description: 'Metrics, logs, traces, and dashboards' },
      { id: 'load-testing', title: 'Load Testing with k6', description: 'Scenarios: baseline, stress, spike, soak' },
    ],
  },
  {
    id: 'api-reference',
    title: 'API Reference',
    icon: FileText,
    children: [
      { id: 'events-api', title: 'Events API', description: 'CRUD operations for events and inventory' },
      { id: 'purchases-api', title: 'Purchases API', description: 'Ticket purchase lifecycle' },
      { id: 'videojobs-api', title: 'Video Jobs API', description: 'Async video processing pipeline' },
      { id: 'payments-api', title: 'Payments API', description: 'Payment processing with resilience' },
      { id: 'webhooks-api', title: 'Webhooks API', description: 'Event delivery and retry logic' },
      { id: 'simulation-api', title: 'Simulation API', description: 'Metrics, health, and failure injection' },
    ],
  },
  {
    id: 'operations',
    title: 'Operations',
    icon: Shield,
    children: [
      { id: 'runbooks', title: 'Runbooks', description: 'Incident response procedures' },
      { id: 'monitoring', title: 'Monitoring & Alerts', description: 'Key metrics and alerting rules' },
      { id: 'scaling', title: 'Scaling Guide', description: 'Horizontal and vertical scaling' },
      { id: 'troubleshooting', title: 'Troubleshooting', description: 'Common issues and solutions' },
    ],
  },
];

const quickstartContent = `
# Quick Start

Get the BackOps Simulator running locally in 5 minutes.

## Prerequisites

- Docker Desktop 4.0+
- .NET 9 SDK
- Node.js 20+
- Git

## 1. Clone the Repository

\`\`\`bash
git clone https://github.com/your-org/backops-simulator.git
cd backops-simulator
\`\`\`

## 2. Start Infrastructure

\`\`\`bash
docker compose up -d postgres redis jaeger seq
\`\`\`

Wait for health checks to pass (about 30 seconds).

## 3. Run Database Migrations

\`\`\`bash
dotnet ef database update --project src/BackOps.Infrastructure --startup-project apps/api
\`\`\`

## 4. Start the API

\`\`\`bash
dotnet run --project apps/api
\`\`\`

API will be available at http://localhost:8080

## 5. Start the Worker

\`\`\`bash
dotnet run --project apps/worker
\`\`\`

## 6. Start the Frontend

\`\`\`bash
cd apps/web
npm install
npm run dev
\`\`\`

Frontend will be available at http://localhost:5173

## 7. Verify Installation

Open http://localhost:5173 and navigate to the Dashboard. You should see:
- System status: Healthy
- All components connected
- Ready to run simulations

## Next Steps

- Try the **High Load Lab** to simulate ticket sales
- Explore the **Payment Gateway Lab** for resilience patterns
- Inject failures in **Chaos Engineering**
- View metrics in **Observability**
`;

const architectureContent = `
# Architecture Overview

## System Context

The BackOps Simulator is a modular monolith designed to demonstrate distributed systems patterns. It consists of:

- **Frontend**: React + TypeScript SPA
- **API**: ASP.NET Core 9 Web API
- **Workers**: Background services for async processing
- **Database**: PostgreSQL for persistence
- **Cache/Queue**: Redis for caching and message queuing
- **Observability**: OpenTelemetry + Grafana + Jaeger + Seq

## Domain-Driven Design

The application follows DDD principles:

\`\`\`
src/
├── BackOps.Domain          # Pure domain logic (no dependencies)
│   ├── Entities           # Aggregates: Event, Ticket, Purchase, Payment, VideoJob, Worker
│   ├── ValueObjects       # Money, IdempotencyKey, CorrelationId
│   ├── Events             # Domain events for cross-aggregate communication
│   ├── Enums              # Domain enumerations
│   ├── Exceptions         # Domain exceptions
│   └── Interfaces         # Repository and service contracts
├── BackOps.Application    # Use cases and application services
│   ├── Commands           # Write operations (CQRS)
│   ├── Queries            # Read operations (CQRS)
│   ├── Handlers           # MediatR handlers
│   ├── Services           # Domain services
│   ├── DTOs               # Data transfer objects
│   └── Validators         # FluentValidation rules
├── BackOps.Infrastructure # External concerns
│   ├── Persistence        # EF Core repositories
│   ├── Messaging          # Redis message bus
│   ├── Resilience         # Circuit breaker, retry, rate limiter, idempotency
│   ├── Observability      # OpenTelemetry metrics and tracing
│   └── HealthChecks       # Custom health checks
└── BackOps.Contracts      # Shared contracts (if needed)
\`\`\`

## Key Patterns

### CQRS with MediatR
Commands and queries are separated for better scalability and maintainability.

### Domain Events
Aggregates raise domain events that are published to the message bus for eventual consistency.

### Idempotency
All mutating operations accept an \`IdempotencyKey\` to prevent duplicate processing.

### Resilience
- **Retry**: Exponential backoff with jitter
- **Circuit Breaker**: Prevents cascade failures
- **Rate Limiting**: Token bucket algorithm
- **Bulkhead**: Worker concurrency limits
- **Fallback**: Graceful degradation

## Data Flow

1. Client sends request with \`X-Correlation-ID\`
2. API validates and publishes command to MediatR
3. Handler executes domain logic
4. Domain events raised and published to Redis
5. Workers consume events and process asynchronously
6. Results persisted to PostgreSQL
7. Metrics emitted to OpenTelemetry
8. Response returned to client
`;

const apiReferenceContent = `
# API Reference

Base URL: \`http://localhost:8080/api\`

All endpoints accept and return JSON. Include \`X-Correlation-ID\` header for tracing.

## Events

### GET /events
List all events
- Query: \`activeOnly=true\`

### GET /events/{id}
Get event details

### GET /events/{eventId}/inventory
Get ticket availability

### POST /events
Create event
\`\`\`json
{
  "name": "Concert 2024",
  "description": "Annual concert",
  "eventDate": "2024-12-31T20:00:00Z",
  "totalCapacity": 10000,
  "ticketPrice": 150.00,
  "currency": "BRL"
}
\`\`\`

### PUT /events/{id}
Update event

### POST /events/{id}/activate
Activate event

### POST /events/{id}/deactivate
Deactivate event

## Purchases

### GET /purchases
List purchases
- Query: \`eventId\`, \`userId\`

### GET /purchases/{id}
Get purchase details

### POST /purchases
Create purchase
\`\`\`json
{
  "eventId": "guid",
  "userId": "guid",
  "quantity": 2,
  "idempotencyKey": "unique-key"
}
\`\`\`

### POST /purchases/{id}/process
Process purchase (reserves tickets)

### POST /purchases/{id}/complete
Complete purchase (confirms tickets)

### POST /purchases/{id}/fail
Fail purchase
\`\`\`json
{ "reason": "Insufficient inventory" }
\`\`\`

### POST /purchases/{id}/cancel
Cancel purchase

## Video Jobs

### GET /videojobs
List video jobs
- Query: \`status\`

### GET /videojobs/queued
Get queued jobs
- Query: \`count=100\`

### GET /videojobs/queue-depth
Get queue depth

### POST /videojobs
Create video job
\`\`\`json
{
  "videoId": "vid-123",
  "videoSizeBytes": 500000000,
  "durationSeconds": 1800,
  "operation": "TRANSCODE",
  "priority": "NORMAL"
}
\`\`\`

### POST /videojobs/{id}/assign
Assign to worker

### POST /videojobs/{id}/complete
Complete job

### POST /videojobs/{id}/fail
Fail job

### POST /videojobs/{id}/retry
Retry failed job

### POST /videojobs/{id}/cancel
Cancel job

## Payments

### GET /payments
List payments
- Query: \`status\`

### GET /payments/{id}
Get payment details

### GET /payments/by-reference/{reference}
Get by external reference

### GET /payments/by-idempotency/{key}
Get by idempotency key

### POST /payments
Create payment
\`\`\`json
{
  "amount": 100.00,
  "currency": "BRL",
  "description": "Order #123",
  "payerEmail": "user@example.com",
  "payerName": "John Doe",
  "cardToken": "tok_visa",
  "idempotencyKey": "idem-123"
}
\`\`\`

### POST /payments/{id}/process
Process payment (invokes bank simulator)

### POST /payments/{id}/authorize
Authorize payment

### POST /payments/{id}/capture
Capture authorized payment

### POST /payments/{id}/complete
Complete payment

### POST /payments/{id}/fail
Fail payment

### POST /payments/{id}/refund
Refund payment

### POST /payments/{id}/retry
Retry failed payment

## Webhooks

### GET /webhooks
List webhooks
- Query: \`paymentId\`

### GET /webhooks/{id}
Get webhook details

### POST /webhooks/{id}/send
Send webhook

### POST /webhooks/{id}/retry
Retry failed webhook

## Simulation

### GET /simulation/metrics
Get simulation metrics
- Query: \`scenario=ticket-sale|streaming|payment-processing|chaos-engineering\`

### GET /simulation/queue-metrics
Get queue metrics
- Query: \`queueName=backops:payment\`

### GET /simulation/health
System health check

### GET /simulation/snapshot
Full metrics snapshot

## Failure Injection

### GET /failure-injection
List active failures

### POST /failure-injection
Inject failure
\`\`\`json
{
  "failureType": "database-latency",
  "rate": 0.3,
  "durationMs": 10000
}
\`\`\`

### DELETE /failure-injection/{type}
Clear specific failure

### DELETE /failure-injection
Clear all failures

## Circuit Breaker

### GET /circuit-breaker
List all circuit breakers

### GET /circuit-breaker/{name}
Get circuit breaker status

### POST /circuit-breaker
Configure circuit breaker
\`\`\`json
{
  "name": "bank",
  "failureThreshold": 5,
  "timeout": "00:00:30",
  "samplingDuration": 10
}
\`\`\`

## Rate Limiting

### GET /rate-limit/{key}
Get rate limit info

### POST /rate-limit
Configure rate limit
\`\`\`json
{
  "key": "api",
  "limit": 100,
  "window": "00:01:00"
}
\`\`\`
`;

export default function Documentation() {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeSection, setActiveSection] = useState('getting-started');
  const [activeDoc, setActiveDoc] = useState('quickstart');

  const filteredSections = docsSections.map(section => ({
    ...section,
    children: section.children.filter(child =>
      child.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      child.description.toLowerCase().includes(searchQuery.toLowerCase())
    )
  })).filter(section => section.children.length > 0);

  const getContent = (docId: string) => {
    switch (docId) {
      case 'quickstart': return quickstartContent;
      case 'architecture': return architectureContent;
      case 'api-reference': return apiReferenceContent;
      default: return `# ${docId}\n\nDocumentation coming soon...`;
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Documentation</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Comprehensive guides and API reference</p>
        </div>
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
          <Input
            placeholder="Search documentation..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        <aside className="lg:col-span-1 space-y-4">
          {filteredSections.map((section) => (
            <Card key={section.id} className={clsx(activeSection === section.id && 'ring-2 ring-purple-500')}>
              <CardHeader className="pb-2" onClick={() => setActiveSection(section.id)}>
                <div className="flex items-center gap-2 cursor-pointer">
                  <section.icon className={clsx('w-5 h-5', activeSection === section.id ? 'text-purple-500' : 'text-gray-400')} />
                  <span className={clsx('font-medium', activeSection === section.id ? 'text-purple-600 dark:text-purple-400' : 'text-gray-900 dark:text-white')}>
                    {section.title}
                  </span>
                  {activeSection === section.id ? <ChevronDown className="w-4 h-4 ml-auto text-purple-500" /> : <ChevronRight className="w-4 h-4 ml-auto text-gray-400" />}
                </div>
              </CardHeader>
              {activeSection === section.id && (
                <CardContent className="pt-0">
                  <ul className="space-y-1">
                    {section.children.map((doc) => (
                      <li key={doc.id}>
                        <button
                          onClick={() => setActiveDoc(doc.id)}
                          className={clsx(
                            'w-full text-left px-2 py-1.5 rounded text-sm transition-colors',
                            activeDoc === doc.id
                              ? 'bg-purple-50 dark:bg-purple-900/20 text-purple-600 dark:text-purple-400 font-medium'
                              : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                          )}
                        >
                          {doc.title}
                        </button>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              )}
            </Card>
          ))}
        </aside>

        <div className="lg:col-span-3">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle>{docsSections.flatMap(s => s.children).find(d => d.id === activeDoc)?.title || 'Documentation'}</CardTitle>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {docsSections.flatMap(s => s.children).find(d => d.id === activeDoc)?.description}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm">
                  <Copy className="w-4 h-4 mr-2" />
                  Copy
                </Button>
                <Button variant="outline" size="sm">
                  <ExternalLink className="w-4 h-4 mr-2" />
                  View on GitHub
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <MarkdownContent content={getContent(activeDoc)} />
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

function MarkdownContent({ content }: { content: string }) {
  const lines = content.split('\n');
  return (
    <div className="prose prose-gray dark:prose-invert max-w-none space-y-4">
      {lines.map((line, index) => {
        if (line.startsWith('### ')) {
          return <h3 key={index} className="text-lg font-semibold text-gray-900 dark:text-white mt-6 mb-3">{line.slice(4)}</h3>;
        }
        if (line.startsWith('## ')) {
          return <h2 key={index} className="text-xl font-bold text-gray-900 dark:text-white mt-8 mb-4">{line.slice(3)}</h2>;
        }
        if (line.startsWith('# ')) {
          return <h1 key={index} className="text-2xl font-bold text-gray-900 dark:text-white mt-8 mb-4">{line.slice(2)}</h1>;
        }
        if (line.startsWith('```')) {
          return <CodeBlock key={index} lines={lines} index={index} />;
        }
        if (line.startsWith('- ')) {
          return <li key={index} className="ml-4">{line.slice(2)}</li>;
        }
        if (line.trim() === '') {
          return <br key={index} />;
        }
        return <p key={index} className="text-gray-600 dark:text-gray-400 leading-relaxed">{line}</p>;
      })}
    </div>
  );
}

function CodeBlock({ lines, index }: { lines: string[]; index: number }) {
  const language = lines[index].slice(3);
  let codeLines = [];
  let i = index + 1;
  while (i < lines.length && lines[i] !== '```') {
    codeLines.push(lines[i]);
    i++;
  }
  return (
    <div key={index} className="bg-gray-900 dark:bg-gray-950 rounded-lg p-4 overflow-x-auto my-4">
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs text-gray-400 uppercase">{language || 'bash'}</span>
        <Button variant="ghost" size="sm">
          <Copy className="w-4 h-4" />
        </Button>
      </div>
      <pre className="text-sm text-gray-100 font-mono leading-relaxed">{codeLines.join('\n')}</pre>
    </div>
  );
}