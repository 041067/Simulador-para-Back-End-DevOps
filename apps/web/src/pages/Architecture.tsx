import { Server, Database, Layers, GitBranch, Network, Zap, Shield, Eye, Code, Box, ArrowRightLeft, Users, HardDrive, Cpu, Monitor } from 'lucide-react';
import { clsx } from 'clsx';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/Tabs';

const architectureLayers = [
  { name: 'Frontend', icon: Monitor, description: 'React + TypeScript + Vite', tech: ['React 18', 'TypeScript', 'Tailwind CSS', 'TanStack Query', 'Recharts'], color: 'blue' },
  { name: 'API Gateway', icon: Server, description: 'ASP.NET Core 9 Web API', tech: ['Minimal APIs', 'OpenAPI/Swagger', 'Health Checks', 'Rate Limiting', 'Correlation IDs'], color: 'purple' },
  { name: 'Application Services', icon: Layers, description: 'MediatR + CQRS + Domain Services', tech: ['MediatR', 'FluentValidation', 'Result Pattern', 'Domain Events'], color: 'green' },
  { name: 'Domain Layer', icon: Code, description: 'Rich Domain Models + Events', tech: ['Entities', 'Value Objects', 'Domain Events', 'Aggregates'], color: 'orange' },
  { name: 'Infrastructure', icon: HardDrive, description: 'EF Core + Redis + Observability', tech: ['PostgreSQL', 'Redis Streams', 'OpenTelemetry', 'Polly', 'Serilog'], color: 'red' },
  { name: 'Workers', icon: Cpu, description: 'Background Job Processors', tech: ['Video Processing', 'Purchase Processor', 'Webhook Delivery', 'Health Monitoring'], color: 'yellow' },
];

const infrastructureComponents = [
  { name: 'PostgreSQL', icon: Database, description: 'Primary data store', details: 'Events, Tickets, Purchases, Payments, Video Jobs, Workers', port: 5432 },
  { name: 'Redis', icon: Zap, description: 'Cache + Message Queue', details: 'Idempotency keys, Rate limiting, Queue (Streams), Pub/Sub', port: 6379 },
  { name: 'Jaeger', icon: Network, description: 'Distributed Tracing', details: 'OpenTelemetry collector, Trace visualization, Service map', port: 16686 },
  { name: 'Seq', icon: Monitor, description: 'Structured Logging', details: 'Serilog sink, Log search, Dashboards, Alerts', port: 5341 },
  { name: 'Grafana', icon: BarChart2, description: 'Metrics & Dashboards', details: 'Prometheus data source, Custom dashboards, Alerting', port: 3000 },
  { name: 'Prometheus', icon: Monitor, description: 'Metrics Collection', details: 'OpenTelemetry metrics, Service discovery, Retention', port: 9090 },
];

const deploymentTargets = [
  { name: 'Vercel', icon: Globe, description: 'Frontend hosting', tier: 'Free', features: ['Edge network', 'Auto deploy', 'Preview deployments', 'Custom domains'] },
  { name: 'Render', icon: Server, description: 'Backend hosting', tier: 'Free', features: ['Auto deploy from Git', 'Health checks', 'Custom domains', 'SSL included'] },
  { name: 'Supabase', icon: Database, description: 'PostgreSQL database', tier: 'Free', features: ['500 MB database', '1 GB file storage', 'Realtime subscriptions', 'Auth built-in'] },
  { name: 'Upstash', icon: Zap, description: 'Redis serverless', tier: 'Free', features: ['256 MB storage', '500K commands/month', '10 GB bandwidth', 'Global replication'] },
  { name: 'Docker', icon: Box, description: 'Local development', tier: 'Local', features: ['Full stack locally', 'Kafka support', 'All observability', 'No cloud costs'] },
];

const adrs = [
  { id: 'ADR-001', title: 'Use PostgreSQL as Primary Database', status: 'Accepted', date: '2024-01-15', summary: 'PostgreSQL chosen for ACID compliance, JSON support, and free tier availability on Supabase.' },
  { id: 'ADR-002', title: 'Use Redis for Queue and Caching', status: 'Accepted', date: '2024-01-15', summary: 'Redis Streams provides lightweight queue with consumer groups. Upstash offers generous free tier.' },
  { id: 'ADR-003', title: 'Idempotency Strategy with Redis', status: 'Accepted', date: '2024-01-20', summary: 'Idempotency keys stored in Redis with 24h TTL. Prevents duplicate payments and purchases.' },
  { id: 'ADR-004', title: 'Retry Policy with Exponential Backoff', status: 'Accepted', date: '2024-01-22', summary: 'Polly-based retry with jitter. Max 3 retries, base 100ms, multiplier 2x.' },
  { id: 'ADR-005', title: 'Circuit Breaker for External Dependencies', status: 'Accepted', date: '2024-01-25', summary: 'Circuit breaker pattern for bank simulator. Opens after 5 failures, half-open after 30s.' },
  { id: 'ADR-006', title: 'Observability Stack: OpenTelemetry + Grafana', status: 'Accepted', date: '2024-02-01', summary: 'Vendor-neutral instrumentation. Jaeger for traces, Prometheus for metrics, Grafana for dashboards.' },
  { id: 'ADR-007', title: 'Modular Monolith First', status: 'Accepted', date: '2024-02-05', summary: 'Start with modular monolith. Extract services only when justified by team/scale needs.' },
  { id: 'ADR-008', title: 'Kafka as Advanced Lab Option', status: 'Proposed', date: '2024-02-10', summary: 'Kafka available via Docker for advanced labs. Redis Streams for free tier production.' },
];

export default function Architecture() {
  const [activeTab, setActiveTab] = useState('overview');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Architecture</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">System design, infrastructure, and architectural decisions</p>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="infrastructure">Infrastructure</TabsTrigger>
          <TabsTrigger value="deployment">Deployment</TabsTrigger>
          <TabsTrigger value="adrs">ADRs</TabsTrigger>
        </TabsList>

        <TabsContent value="overview">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {architectureLayers.map((layer) => (
              <Card key={layer.name} className="h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className={clsx('p-3 rounded-lg', `bg-${layer.color}-100 text-${layer.color}-600 dark:bg-${layer.color}-900/30 dark:text-${layer.color}-400`)}>
                      <layer.icon className="w-6 h-6" />
                    </div>
                    <div>
                      <CardTitle>{layer.name}</CardTitle>
                      <p className="text-sm text-gray-500 dark:text-gray-400">{layer.description}</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {layer.tech.map((t) => (
                      <li key={t} className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                        <span className="w-1.5 h-1.5 rounded-full bg-gray-400" />
                        {t}
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <GitBranch className="w-5 h-5" />
                Data Flow
              </CardTitle>
            </CardHeader>
            <CardContent>
              <DataFlowDiagram />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="infrastructure">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {infrastructureComponents.map((comp) => (
              <Card key={comp.name} className="h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-3 rounded-lg bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400">
                      <comp.icon className="w-6 h-6" />
                    </div>
                    <div>
                      <CardTitle>{comp.name}</CardTitle>
                      <p className="text-sm text-gray-500 dark:text-gray-400">{comp.description}</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{comp.details}</p>
                  <div className="flex items-center gap-2 text-sm">
                    <Monitor className="w-4 h-4 text-gray-400" />
                    <span className="font-mono">Port {comp.port}</span>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Shield className="w-5 h-5" />
                Security Considerations
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div className="space-y-3">
                  <h4 className="font-medium text-gray-900 dark:text-white">Implemented</h4>
                  <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                    <SecurityItem icon={CheckCircle} text="Correlation IDs for request tracing" />
                    <SecurityItem icon={CheckCircle} text="Rate limiting on all public endpoints" />
                    <SecurityItem icon={CheckCircle} text="Idempotency keys prevent duplicate charges" />
                    <SecurityItem icon={CheckCircle} text="Circuit breakers prevent cascade failures" />
                    <SecurityItem icon={CheckCircle} text="Structured logging with Serilog" />
                    <SecurityItem icon={CheckCircle} text="Health checks for all dependencies" />
                  </ul>
                </div>
                <div className="space-y-3">
                  <h4 className="font-medium text-gray-900 dark:text-white">Planned</h4>
                  <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                    <SecurityItem icon={Shield} text="Authentication (JWT/OIDC)" />
                    <SecurityItem icon={Shield} text="Authorization (RBAC)" />
                    <SecurityItem icon={Shield} text="API key management" />
                    <SecurityItem icon={Shield} text="Audit logging for sensitive operations" />
                    <SecurityItem icon={Shield} text="Secret rotation automation" />
                    <SecurityItem icon={Shield} text="WAF rules for OWASP Top 10" />
                  </ul>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="deployment">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {deploymentTargets.map((target) => (
              <Card key={target.name} className="h-full">
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="p-3 rounded-lg bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400">
                        <target.icon className="w-6 h-6" />
                      </div>
                      <div>
                        <CardTitle>{target.name}</CardTitle>
                        <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', target.tier === 'Free' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400')}>
                          {target.tier}
                        </span>
                      </div>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">{target.description}</p>
                  <ul className="space-y-1">
                    {target.features.map((f) => (
                      <li key={f} className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
                        <CheckCircle className="w-4 h-4 text-green-500" />
                        {f}
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            ))}
          </div>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ArrowRightLeft className="w-5 h-5" />
                CI/CD Pipeline
              </CardTitle>
            </CardHeader>
            <CardContent>
              <CICDPipeline />
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="adrs">
          <div className="space-y-4">
            {adrs.map((adr) => (
              <Card key={adr.id}>
                <CardContent className="py-4">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <span className="font-mono text-sm text-purple-600 dark:text-purple-400">{adr.id}</span>
                        <h4 className="font-medium text-gray-900 dark:text-white">{adr.title}</h4>
                        <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', adr.status === 'Accepted' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400')}>
                          {adr.status}
                        </span>
                        <span className="text-sm text-gray-500 dark:text-gray-400">{adr.date}</span>
                      </div>
                      <p className="text-gray-600 dark:text-gray-400">{adr.summary}</p>
                    </div>
                    <Button variant="ghost" size="sm">View Details</Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function DataFlowDiagram() {
  return (
    <div className="overflow-x-auto">
      <div className="flex items-center justify-center gap-2 py-8 px-4">
        {[
          { label: 'Client', icon: Users, color: 'blue' },
          { label: 'Vercel\nCDN', icon: Globe, color: 'gray' },
          { label: 'API Gateway\n(ASP.NET Core)', icon: Server, color: 'purple' },
          { label: 'Redis\nQueue/Cache', icon: Zap, color: 'orange' },
          { label: 'Workers', icon: Cpu, color: 'green' },
          { label: 'PostgreSQL', icon: Database, color: 'blue' },
          { label: 'Observability', icon: Monitor, color: 'red' },
        ].map((step, index) => (
          <div key={step.label} className="flex flex-col items-center">
            <div className={clsx('relative flex flex-col items-center', `border-${step.color}-500`)}>
              <div className={clsx('w-20 h-20 rounded-xl flex items-center justify-center text-white font-medium text-sm', `bg-${step.color}-500`)}>
                <step.icon className="w-8 h-8 mb-1" />
                <div className="text-center leading-tight">{step.label}</div>
              </div>
            </div>
            {index < 6 && (
              <div className="w-8 flex justify-center">
                <svg className="w-6 h-6 text-gray-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M5 12h14M12 5l7 7-7 7" />
                </svg>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function CICDPipeline() {
  const stages = [
    { name: 'Code Push', icon: GitBranch, color: 'gray' },
    { name: 'Build', icon: Box, color: 'blue' },
    { name: 'Unit Tests', icon: CheckCircle, color: 'green' },
    { name: 'Integration Tests', icon: CheckCircle, color: 'green' },
    { name: 'Architecture Tests', icon: Shield, color: 'purple' },
    { name: 'Security Scan', icon: Shield, color: 'red' },
    { name: 'Docker Build', icon: Box, color: 'blue' },
    { name: 'Deploy to Staging', icon: Server, color: 'yellow' },
    { name: 'Smoke Tests', icon: CheckCircle, color: 'green' },
    { name: 'Deploy to Prod', icon: Server, color: 'red' },
  ];

  return (
    <div className="overflow-x-auto">
      <div className="flex items-center gap-2 py-4 px-4">
        {stages.map((stage, index) => (
          <div key={stage.name} className="flex flex-col items-center min-w-[100px]">
            <div className={clsx('w-16 h-16 rounded-xl flex items-center justify-center text-white font-medium text-xs', `bg-${stage.color}-500`)}>
              <stage.icon className="w-6 h-6 mb-1" />
              <div className="text-center leading-tight">{stage.name}</div>
            </div>
            {index < stages.length - 1 && (
              <div className="w-6 flex justify-center">
                <svg className="w-5 h-5 text-gray-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M5 12h14M12 5l7 7-7 7" />
                </svg>
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function SecurityItem({ icon: Icon, text }: { icon: any; text: string }) {
  return (
    <li className="flex items-center gap-2">
      <Icon className="w-4 h-4 text-green-500" />
      <span>{text}</span>
    </li>
  );
}