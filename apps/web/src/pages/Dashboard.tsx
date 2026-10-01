import { useQuery } from '@tanstack/react-query';
import { LayoutDashboard, Ticket, Video, CreditCard, Bug, Activity, TrendingUp, Server, Database, Users, Clock, AlertTriangle, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { MetricCard } from '@/components/MetricCard';
import { QueueChart } from '@/components/QueueChart';
import { WorkerStatus } from '@/components/WorkerStatus';
import { api } from '@/utils/api';

interface SystemMetrics {
  requests: { total: number; successful: number; failed: number; queued: number };
  throughput: number;
  avgLatency: number;
  p95Latency: number;
  p99Latency: number;
  queue: { pending: number; processing: number; completed: number; failed: number };
  workers: Array<{
    id: string;
    name: string;
    type: string;
    status: string;
    maxConcurrency: number;
    currentJobs: number;
    totalJobsProcessed: number;
    totalJobsFailed: number;
    utilization: number;
    lastHeartbeat: string | null;
  }>;
}

interface HealthCheck {
  status: string;
  timestamp: string;
  components: Record<string, string>;
}

export default function Dashboard() {
  const { data: metrics, isLoading: metricsLoading } = useQuery<SystemMetrics>({
    queryKey: ['metrics'],
    queryFn: () => api.get('/api/simulation/snapshot').then(r => r.data),
    refetchInterval: 5000,
  });

  const { data: health, isLoading: healthLoading } = useQuery<HealthCheck>({
    queryKey: ['health'],
    queryFn: () => api.get('/api/simulation/health').then(r => r.data),
    refetchInterval: 30000,
  });

  const metricCards = [
    { label: 'Total Requests', value: metrics?.requests.total.toLocaleString() || '0', icon: Activity, color: 'blue', trend: '+12%' },
    { label: 'Successful', value: metrics?.requests.successful.toLocaleString() || '0', icon: CheckCircle, color: 'green', trend: '+8%' },
    { label: 'Failed', value: metrics?.requests.failed.toLocaleString() || '0', icon: XCircle, color: 'red', trend: '-2%' },
    { label: 'Queued', value: metrics?.requests.queued.toLocaleString() || '0', icon: Clock, color: 'yellow', trend: '+5%' },
    { label: 'Throughput (req/s)', value: metrics?.throughput.toFixed(1) || '0', icon: TrendingUp, color: 'purple', trend: '+15%' },
    { label: 'Avg Latency', value: `${metrics?.avgLatency.toFixed(0) || '0'}ms`, icon: Clock, color: 'orange', trend: '-10%' },
    { label: 'P95 Latency', value: `${metrics?.p95Latency.toFixed(0) || '0'}ms`, icon: Clock, color: 'orange', trend: '-5%' },
    { label: 'P99 Latency', value: `${metrics?.p99Latency.toFixed(0) || '0'}ms`, icon: Clock, color: 'red', trend: '-3%' },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Dashboard</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Real-time system metrics and observability</p>
        </div>
        <div className="flex items-center gap-3">
          <span className={clsx('px-3 py-1 rounded-full text-sm font-medium', health?.status === 'Healthy' ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400')}>
            {health?.status || 'Unknown'}
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-8 gap-4">
        {metricCards.map((card, index) => (
          <MetricCard key={index} {...card} />
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Database className="w-5 h-5" />
              Queue Status
            </CardTitle>
          </CardHeader>
          <CardContent>
            <QueueChart data={metrics?.queue} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Server className="w-5 h-5" />
              Worker Pool
            </CardTitle>
          </CardHeader>
          <CardContent>
            <WorkerStatus workers={metrics?.workers || []} />
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Ticket className="w-5 h-5" />
              High Load Lab
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="text-gray-600 dark:text-gray-400 text-sm">Simulate ticket sales under extreme concurrency</p>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Events</p>
                <p className="font-bold text-lg">3</p>
              </div>
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Active Purchases</p>
                <p className="font-bold text-lg">{metrics?.requests.queued || 0}</p>
              </div>
            </div>
            <a href="/high-load" className="block text-center text-purple-600 dark:text-purple-400 hover:underline text-sm font-medium">Open Lab →</a>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Video className="w-5 h-5" />
              Streaming Lab
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="text-gray-600 dark:text-gray-400 text-sm">Video processing pipeline with backpressure</p>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Queued Jobs</p>
                <p className="font-bold text-lg">{metrics?.queue.pending || 0}</p>
              </div>
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Processing</p>
                <p className="font-bold text-lg">{metrics?.queue.processing || 0}</p>
              </div>
            </div>
            <a href="/streaming" className="block text-center text-purple-600 dark:text-purple-400 hover:underline text-sm font-medium">Open Lab →</a>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CreditCard className="w-5 h-5" />
              Payment Gateway Lab
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="text-gray-600 dark:text-gray-400 text-sm">Idempotency, retries, circuit breakers</p>
            <div className="grid grid-cols-2 gap-2 text-sm">
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Total Payments</p>
                <p className="font-bold text-lg">{metrics?.requests.total || 0}</p>
              </div>
              <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-lg">
                <p className="text-gray-500 dark:text-gray-400">Success Rate</p>
                <p className="font-bold text-lg">
                  {metrics?.requests.total > 0
                    ? `${((metrics.requests.successful / metrics.requests.total) * 100).toFixed(1)}%`
                    : '0%'}
                </p>
              </div>
            </div>
            <a href="/payments" className="block text-center text-purple-600 dark:text-purple-400 hover:underline text-sm font-medium">Open Lab →</a>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}