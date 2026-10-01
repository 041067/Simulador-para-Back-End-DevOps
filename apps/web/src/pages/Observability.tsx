import { useQuery } from '@tanstack/react-query';
import { Activity, BarChart2, Server, Database, Zap, AlertTriangle, CheckCircle, XCircle, Clock, TrendingUp, Download, ExternalLink } from 'lucide-react';
import { clsx } from 'clsx';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { api, simulationApi } from '@/utils/api';
import { MetricsSnapshot, HealthCheck } from '@/types/simulation';

const statusColors = {
  Healthy: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  Degraded: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',
  Unhealthy: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
};

export default function Observability() {
  const { data: snapshot } = useQuery({
    queryKey: ['metrics-snapshot'],
    queryFn: () => simulationApi.getSnapshot().then(r => r.data),
    refetchInterval: 5000,
  });

  const { data: health } = useQuery({
    queryKey: ['health'],
    queryFn: () => simulationApi.getHealth().then(r => r.data),
    refetchInterval: 10000,
  });

  const timeRangeOptions = [
    { value: '1m', label: 'Last Minute' },
    { value: '5m', label: 'Last 5 Minutes' },
    { value: '15m', label: 'Last 15 Minutes' },
    { value: '1h', label: 'Last Hour' },
    { value: '6h', label: 'Last 6 Hours' },
    { value: '24h', label: 'Last 24 Hours' },
  ];

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Observability</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Real-time metrics, distributed tracing, and system health</p>
        </div>
        <div className="flex items-center gap-3">
          <Select
            value="5m"
            onValueChange={() => {}}
            options={timeRangeOptions}
            className="w-48"
          />
          <Button variant="outline" size="sm">
            <Download className="w-4 h-4 mr-2" />
            Export
          </Button>
          <Button variant="outline" size="sm">
            <ExternalLink className="w-4 h-4 mr-2" />
            Grafana
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">System Status</CardTitle>
            <Activity className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-3">
              <span className={clsx('px-3 py-1 rounded-full text-sm font-medium', statusColors[health?.status as keyof typeof statusColors] || statusColors.Healthy)}>
                {health?.status || 'Unknown'}
              </span>
              <span className="text-sm text-gray-500 dark:text-gray-400">
                Updated {health?.timestamp ? new Date(health.timestamp).toLocaleTimeString() : 'never'}
              </span>
            </div>
            <div className="mt-4 grid grid-cols-2 gap-4 text-sm">
              {health?.components && Object.entries(health.components).map(([key, value]) => (
                <div key={key} className="flex items-center gap-2">
                  <span className={clsx('w-2 h-2 rounded-full', value === 'healthy' ? 'bg-green-500' : value === 'degraded' ? 'bg-yellow-500' : 'bg-red-500')} />
                  <span className="capitalize">{key.replace(/_/g, ' ')}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Requests/sec</CardTitle>
            <TrendingUp className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-gray-900 dark:text-white">
              {snapshot?.requestsPerSecond.toFixed(1) || '0'}
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Current throughput</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Error Rate</CardTitle>
            <XCircle className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-red-600 dark:text-red-400">
              {snapshot?.errorRate.toFixed(2) || '0'}%
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Percentage of failed requests</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Active Workers</CardTitle>
            <Server className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-gray-900 dark:text-white">
              {snapshot?.activeWorkers || 0}
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Processing jobs</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="w-5 h-5" />
              Latency Percentiles
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <LatencyBar label="P50 (Median)" value={snapshot?.p50LatencyMs || 0} max={snapshot?.p99LatencyMs || 100} color="#22c55e" />
              <LatencyBar label="P95" value={snapshot?.p95LatencyMs || 0} max={snapshot?.p99LatencyMs || 100} color="#f59e0b" />
              <LatencyBar label="P99" value={snapshot?.p99LatencyMs || 0} max={snapshot?.p99LatencyMs || 100} color="#ef4444" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BarChart2 className="w-5 h-5" />
              Queue & Circuit Breakers
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Queue Depth</span>
                <span className="text-lg font-bold text-gray-900 dark:text-white">{snapshot?.queueDepth || 0}</span>
              </div>
              <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                <div
                  className="h-full rounded-full bg-purple-600 transition-all duration-300"
                  style={{ width: `${Math.min(100, (snapshot?.queueDepth || 0) / 10000 * 100)}%` }}
                />
              </div>

              <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
                <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Circuit Breakers</h4>
                <div className="space-y-2">
                  {snapshot?.circuitBreakers?.map((cb: any) => (
                    <div key={cb.name} className="flex items-center justify-between text-sm">
                      <span className="font-medium">{cb.name}</span>
                      <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', cb.state === 'open' ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400' : cb.state === 'half-open' ? 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400' : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400')}>
                        {cb.state.toUpperCase()}
                      </span>
                    </div>
                  ))}
                  {(!snapshot?.circuitBreakers || snapshot.circuitBreakers.length === 0) && (
                    <p className="text-sm text-gray-500 dark:text-gray-400">All circuit breakers closed</p>
                  )}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BarChart2 className="w-5 h-5" />
              Request Rate Over Time
            </CardTitle>
          </CardHeader>
          <CardContent>
            <RequestRateChart />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Database className="w-5 h-5" />
              Component Health
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <ComponentHealth name="API Gateway" status="healthy" latency="12ms" />
              <ComponentHealth name="PostgreSQL" status="healthy" latency="8ms" />
              <ComponentHealth name="Redis" status="healthy" latency="2ms" />
              <ComponentHealth name="Worker Pool" status="healthy" latency="45ms" />
              <ComponentHealth name="Bank Simulator" status="degraded" latency="1.2s" />
              <ComponentHealth name="Webhook Delivery" status="healthy" latency="234ms" />
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="w-5 h-5" />
            Distributed Tracing
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <TraceExample traceId="abc123def456" service="API Gateway" duration="2ms" status="ok" />
            <TraceExample traceId="abc123def456" service="Payment Service" duration="45ms" status="ok" />
            <TraceExample traceId="abc123def456" service="Redis Queue" duration="3ms" status="ok" />
            <TraceExample traceId="abc123def456" service="Worker" duration="120ms" status="ok" />
            <TraceExample traceId="abc123def456" service="Bank Simulator" duration="890ms" status="slow" />
            <TraceExample traceId="abc123def456" service="PostgreSQL" duration="15ms" status="ok" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function LatencyBar({ label, value, max, color }: { label: string; value: number; max: number; color: string }) {
  const percentage = max > 0 ? Math.min(100, (value / max) * 100) : 0;
  return (
    <div className="space-y-1">
      <div className="flex justify-between text-sm">
        <span className="font-medium">{label}</span>
        <span className="font-mono text-gray-900 dark:text-white">{value.toFixed(0)}ms</span>
      </div>
      <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
        <div
          className="h-full rounded-full transition-all duration-500"
          style={{ width: `${percentage}%`, backgroundColor: color }}
        />
      </div>
    </div>
  );
}

function RequestRateChart() {
  const data = Array.from({ length: 20 }, (_, i) => ({
    time: `${19 + Math.floor(i / 2)}:${(i % 2) * 30}`.padStart(5, '0'),
    requests: Math.floor(Math.random() * 500) + 100,
    errors: Math.floor(Math.random() * 10),
  }));

  return (
    <div className="h-64">
      <svg viewBox="0 0 600 256" className="w-full h-full" preserveAspectRatio="none">
        <defs>
          <linearGradient id="requestsGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#8b5cf6" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#8b5cf6" stopOpacity="0" />
          </linearGradient>
          <linearGradient id="errorsGradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#ef4444" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#ef4444" stopOpacity="0" />
          </linearGradient>
        </defs>
        <rect width="600" height="256" fill="transparent" />
        <path
          d={data.map((d, i) => `${i === 0 ? 'M' : 'L'}${i * 30} ${256 - (d.requests / 600) * 200}`).join(' ')}
          stroke="#8b5cf6"
          strokeWidth="2"
          fill="url(#requestsGradient)"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d={data.map((d, i) => `${i === 0 ? 'M' : 'L'}${i * 30} ${256 - (d.errors / 20) * 200}`).join(' ')}
          stroke="#ef4444"
          strokeWidth="2"
          fill="url(#errorsGradient)"
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeDasharray="5,5"
        />
      </svg>
      <div className="flex items-center justify-center gap-4 mt-3 text-sm text-gray-500 dark:text-gray-400">
        <span className="flex items-center gap-1">
          <span className="w-3 h-3 rounded bg-purple-500" />
          Requests
        </span>
        <span className="flex items-center gap-1">
          <span className="w-3 h-3 rounded bg-red-500" />
          Errors
        </span>
      </div>
    </div>
  );
}

function ComponentHealth({ name, status, latency }: { name: string; status: 'healthy' | 'degraded' | 'unhealthy'; latency: string }) {
  const statusConfig = {
    healthy: { color: 'bg-green-500', label: 'Healthy' },
    degraded: { color: 'bg-yellow-500', label: 'Degraded' },
    unhealthy: { color: 'bg-red-500', label: 'Unhealthy' },
  };
  const config = statusConfig[status];
  return (
    <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
      <div className="flex items-center gap-3">
        <span className={clsx('w-3 h-3 rounded-full', config.color)} />
        <span className="font-medium text-gray-900 dark:text-white">{name}</span>
      </div>
      <div className="flex items-center gap-3 text-sm">
        <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', config.color.replace('bg-', 'bg-').replace('500', '100'), config.color.replace('500', '700'))}>
          {config.label}
        </span>
        <span className="font-mono text-gray-500 dark:text-gray-400">{latency}</span>
      </div>
    </div>
  );
}

function TraceExample({ traceId, service, duration, status }: { traceId: string; service: string; duration: string; status: 'ok' | 'slow' | 'error' }) {
  const statusConfig = {
    ok: { color: 'text-green-500', bg: 'bg-green-100 dark:bg-green-900/30' },
    slow: { color: 'text-yellow-500', bg: 'bg-yellow-100 dark:bg-yellow-900/30' },
    error: { color: 'text-red-500', bg: 'bg-red-100 dark:bg-red-900/30' },
  };
  const config = statusConfig[status];
  return (
    <div className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
      <span className="font-mono text-xs text-gray-500 dark:text-gray-400 w-24">{traceId.slice(0, 8)}...</span>
      <div className="flex-1 h-1 bg-gray-200 dark:bg-gray-700 rounded-full relative overflow-hidden">
        <div
          className="h-full rounded-full"
          style={{ width: `${Math.min(100, parseFloat(duration) / 1000 * 100)}%`, backgroundColor: config.color.replace('text-', '') }}
        />
      </div>
      <span className="font-medium text-gray-900 dark:text-white w-32">{service}</span>
      <span className={clsx('px-2 py-1 rounded-full text-xs font-mono', config.bg, config.color)}>{duration}</span>
    </div>
  );
}