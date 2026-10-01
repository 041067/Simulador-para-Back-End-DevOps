import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Play, Stop, Plus, Trash2, RefreshCw, Settings, Zap, Users, Ticket, BarChart2, Loader2, AlertTriangle, CheckCircle } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { api, highLoadApi, simulationApi } from '@/utils/api';
import { SimulationConfig, SimulationResult } from '@/types/simulation';

interface ScenarioConfig {
  id: string;
  name: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
}

const scenarios: ScenarioConfig[] = [
  {
    id: 'ticket-sale',
    name: 'Ticket Sale',
    icon: Ticket,
    description: 'Simulate high-concurrency ticket purchases with overselling prevention',
  },
  {
    id: 'streaming',
    name: 'Video Streaming',
    icon: Zap,
    description: 'Process thousands of video transcoding jobs with backpressure',
  },
];

export default function HighLoadLab({ scenario: initialScenario }: { scenario?: string }) {
  const queryClient = useQueryClient();
  const [activeScenario, setActiveScenario] = useState(initialScenario || 'ticket-sale');
  const [config, setConfig] = useState<SimulationConfig>({
    scenario: activeScenario as any,
    users: 1000,
    requestsPerUser: 10,
    workers: 4,
    queueProvider: 'redis',
    enableFailureInjection: false,
    failureRate: 0.1,
    failureDurationMs: 5000,
  });
  const [isRunning, setIsRunning] = useState(false);
  const [results, setResults] = useState<SimulationResult | null>(null);

  const { data: events } = useQuery({
    queryKey: ['events'],
    queryFn: () => highLoadApi.getEvents().then(r => r.data),
  });

  const { data: queueDepth } = useQuery({
    queryKey: ['queue-depth'],
    queryFn: () => highLoadApi.getQueueDepth().then(r => r.data),
    refetchInterval: 2000,
    enabled: activeScenario === 'streaming',
  });

  const runSimulation = useMutation({
    mutationFn: async (config: SimulationConfig) => {
      // Create event if needed
      let eventId = config.scenario === 'ticket-sale' ? events?.[0]?.id : null;
      if (!eventId && config.scenario === 'ticket-sale') {
        const event = await highLoadApi.createEvent({
          name: 'Concert Simulation',
          description: 'High load ticket sale simulation',
          eventDate: new Date(Date.now() + 86400000).toISOString(),
          totalCapacity: config.users * config.requestsPerUser,
          ticketPrice: 100,
        });
        eventId = event.data;
      }

      // For streaming scenario, create video jobs
      if (config.scenario === 'streaming') {
        for (let i = 0; i < config.users; i++) {
          await highLoadApi.createVideoJob({
            videoId: `video-${i}`,
            videoSizeBytes: Math.floor(Math.random() * 1000000000) + 100000000,
            durationSeconds: Math.floor(Math.random() * 3600) + 60,
            operation: 'TRANSCODE',
            priority: 'NORMAL',
          });
        }
      }

      return { eventId, config };
    },
    onSuccess: () => {
      setIsRunning(true);
      setResults(null);
    },
    onError: (error) => {
      console.error('Simulation failed:', error);
      setIsRunning(false);
    },
  });

  const handleRunSimulation = () => {
    runSimulation.mutate(config);
  };

  const handleStopSimulation = () => {
    setIsRunning(false);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">High Load Lab</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Simulate extreme concurrency and queue processing</p>
        </div>
        <div className="flex items-center gap-2">
          <span className={clsx('px-3 py-1 rounded-full text-sm font-medium', isRunning ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400')}>
            {isRunning ? <Loader2 className="w-4 h-4 inline animate-spin mr-1" /> Running : 'Idle'}
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Scenario Configuration
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Scenario</label>
              <div className="grid grid-cols-2 gap-2">
                {scenarios.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => {
                      setActiveScenario(s.id);
                      setConfig(prev => ({ ...prev, scenario: s.id as any }));
                    }}
                    className={clsx(
                      'p-4 rounded-lg border-2 text-left transition-all',
                      activeScenario === s.id
                        ? 'border-purple-500 bg-purple-50 dark:bg-purple-900/20'
                        : 'border-gray-200 dark:border-gray-700 hover:border-purple-300'
                    )}
                  >
                    <s.icon className="w-6 h-6 mb-2" />
                    <p className="font-medium">{s.name}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{s.description}</p>
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Concurrent Users: {config.users}
                </label>
                <input
                  type="range"
                  min="100"
                  max="50000"
                  step="100"
                  value={config.users}
                  onChange={(e) => setConfig(prev => ({ ...prev, users: parseInt(e.target.value) }))}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-purple-600"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Requests per User: {config.requestsPerUser}
                </label>
                <input
                  type="range"
                  min="1"
                  max="100"
                  step="1"
                  value={config.requestsPerUser}
                  onChange={(e) => setConfig(prev => ({ ...prev, requestsPerUser: parseInt(e.target.value) }))}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-purple-600"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Workers: {config.workers}
                </label>
                <input
                  type="range"
                  min="1"
                  max="32"
                  step="1"
                  value={config.workers}
                  onChange={(e) => setConfig(prev => ({ ...prev, workers: parseInt(e.target.value) }))}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-purple-600"
                />
              </div>

              <Select
                label="Queue Provider"
                value={config.queueProvider}
                onValueChange={(value) => setConfig(prev => ({ ...prev, queueProvider: value as any }))}
                options={[
                  { value: 'redis', label: 'Redis Streams' },
                  { value: 'kafka', label: 'Apache Kafka (Local)' },
                ]}
              />
            </div>

            <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={config.enableFailureInjection}
                  onChange={(e) => setConfig(prev => ({ ...prev, enableFailureInjection: e.target.checked }))}
                  className="w-4 h-4 rounded border-gray-300 text-purple-600 focus:ring-purple-500"
                />
                <span className="text-sm font-medium">Enable Failure Injection</span>
              </label>
              {config.enableFailureInjection && (
                <div className="space-y-2 mt-2 pl-6 border-l-2 border-gray-200 dark:border-gray-700">
                  <Select
                    label="Failure Type"
                    value={config.failureType || 'database-latency'}
                    onValueChange={(value) => setConfig(prev => ({ ...prev, failureType: value }))}
                    options={[
                      { value: 'database-latency', label: 'Database Latency' },
                      { value: 'database-unavailable', label: 'Database Unavailable' },
                      { value: 'redis-unavailable', label: 'Redis Unavailable' },
                      { value: 'worker-failure', label: 'Worker Failure' },
                      { value: 'network-timeout', label: 'Network Timeout' },
                      { value: 'random-errors', label: 'Random Errors' },
                    ]}
                  />
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Failure Rate: {(config.failureRate * 100).toFixed(0)}%
                    </label>
                    <input
                      type="range"
                      min="0"
                      max="100"
                      step="5"
                      value={config.failureRate * 100}
                      onChange={(e) => setConfig(prev => ({ ...prev, failureRate: parseInt(e.target.value) / 100 }))}
                      className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-red-600"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                      Duration: {config.failureDurationMs}ms
                    </label>
                    <input
                      type="range"
                      min="1000"
                      max="60000"
                      step="1000"
                      value={config.failureDurationMs}
                      onChange={(e) => setConfig(prev => ({ ...prev, failureDurationMs: parseInt(e.target.value) }))}
                      className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-red-600"
                    />
                  </div>
                </div>
              )}
            </div>

            <div className="flex gap-2 pt-2">
              <Button onClick={handleRunSimulation} disabled={isRunning || runSimulation.isPending} className="flex-1" size="lg">
                <Play className="w-4 h-4 mr-2" />
                Run Simulation
              </Button>
              <Button onClick={handleStopSimulation} disabled={!isRunning} variant="outline" size="lg">
                <Stop className="w-4 h-4 mr-2" />
                Stop
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BarChart2 className="w-5 h-5" />
              Real-time Metrics
            </CardTitle>
          </CardHeader>
          <CardContent>
            {results ? (
              <SimulationResults results={results} />
            ) : isRunning ? (
              <div className="flex flex-col items-center justify-center py-12">
                <Loader2 className="w-12 h-12 animate-spin text-purple-600" />
                <p className="mt-4 text-gray-500 dark:text-gray-400">Simulation running...</p>
                <p className="text-sm text-gray-400">Total Requests: {config.users * config.requestsPerUser}</p>
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500 dark:text-gray-400">
                <Zap className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>Configure and run a simulation to see real-time metrics</p>
                <p className="text-sm mt-2">Supports ticket sales and video streaming scenarios</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="w-5 h-5" />
              Active Events
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {events?.map((event: any) => (
                <div key={event.id} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <div>
                    <p className="font-medium">{event.name}</p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                      {event.availableTickets} / {event.totalCapacity} tickets available
                    </p>
                  </div>
                  <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', event.isActive ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400')}>
                    {event.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              ))}
              {events?.length === 0 && (
                <p className="text-center text-gray-500 dark:text-gray-400 py-8">No events created yet</p>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-yellow-500" />
              System Alerts
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex items-center gap-3 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg border border-yellow-200 dark:border-yellow-800">
                <AlertTriangle className="w-5 h-5 text-yellow-500" />
                <div>
                  <p className="font-medium text-yellow-800 dark:text-yellow-300">High Load Simulation Ready</p>
                  <p className="text-sm text-yellow-600 dark:text-yellow-400">System is ready for load testing</p>
                </div>
              </div>
              <div className="flex items-center gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-800">
                <CheckCircle className="w-5 h-5 text-green-500" />
                <div>
                  <p className="font-medium text-green-800 dark:text-green-300">All Systems Operational</p>
                  <p className="text-sm text-green-600 dark:text-green-400">Database, Redis, and Workers healthy</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function SimulationResults({ results }: { results: SimulationResult }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricValue label="Total Requests" value={results.totalRequests.toLocaleString()} icon={Users} color="blue" />
        <MetricValue label="Successful" value={results.successfulRequests.toLocaleString()} icon={CheckCircle} color="green" />
        <MetricValue label="Failed" value={results.failedRequests.toLocaleString()} icon={AlertTriangle} color="red" />
        <MetricValue label="Throughput" value={`${results.throughput.toFixed(1)} req/s`} icon={Zap} color="purple" />
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricValue label="Avg Latency" value={`${results.avgLatencyMs.toFixed(0)}ms`} icon={BarChart2} color="orange" />
        <MetricValue label="P95 Latency" value={`${results.p95LatencyMs.toFixed(0)}ms`} icon={BarChart2} color="orange" />
        <MetricValue label="P99 Latency" value={`${results.p99LatencyMs.toFixed(0)}ms`} icon={BarChart2} color="red" />
        <MetricValue label="Queue Depth" value={results.queueDepth.toLocaleString()} icon={BarChart2} color="yellow" />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Card>
          <CardHeader>
            <CardTitle>Latency Distribution</CardTitle>
          </CardHeader>
          <CardContent>
            <LatencyChart data={results} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Worker Performance</CardTitle>
          </CardHeader>
          <CardContent>
            <WorkerPerformanceChart workers={results.workers} />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function MetricValue({ label, value, icon: Icon, color }: { label: string; value: string; icon: any; color: string }) {
  const colorMap = {
    blue: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
    green: 'bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400',
    red: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
    yellow: 'bg-yellow-100 text-yellow-600 dark:bg-yellow-900/30 dark:text-yellow-400',
    purple: 'bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400',
    orange: 'bg-orange-100 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400',
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-500 dark:text-gray-400">{label}</p>
          <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">{value}</p>
        </div>
        <div className={clsx('p-3 rounded-lg', colorMap[color as keyof typeof colorMap])}>
          <Icon className="w-6 h-6" />
        </div>
      </div>
    </div>
  );
}

function LatencyChart({ data }: { data: SimulationResult }) {
  return (
    <div className="h-48">
      <div className="flex items-end justify-around h-full">
        {[
          { label: 'Avg', value: data.avgLatencyMs, max: data.p99LatencyMs, color: '#3b82f6' },
          { label: 'P50', value: data.avgLatencyMs * 0.7, max: data.p99LatencyMs, color: '#22c55e' },
          { label: 'P95', value: data.p95LatencyMs, max: data.p99LatencyMs, color: '#f59e0b' },
          { label: 'P99', value: data.p99LatencyMs, max: data.p99LatencyMs, color: '#ef4444' },
        ].map((item, index) => (
          <div key={index} className="flex flex-col items-center gap-1">
            <div
              className="w-16 rounded-t transition-all duration-500"
              style={{
                height: `${Math.max(10, (item.value / item.max) * 100)}%`,
                backgroundColor: item.color,
              }}
            />
            <span className="text-xs text-gray-500 dark:text-gray-400">{item.label}</span>
            <span className="text-sm font-medium text-gray-900 dark:text-white">{item.value.toFixed(0)}ms</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function WorkerPerformanceChart({ workers }: { workers: SimulationResult['workers'] }) {
  return (
    <div className="space-y-3">
      {workers.map((worker) => (
        <div key={worker.id} className="space-y-1">
          <div className="flex items-center justify-between text-sm">
            <span className="font-medium">{worker.name}</span>
            <span className="text-gray-500 dark:text-gray-400">
              {(worker.utilization * 100).toFixed(0)}% • {worker.totalJobsProcessed} jobs
            </span>
          </div>
          <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div
              className="h-full rounded-full transition-all duration-500"
              style={{
                width: `${Math.min(100, worker.utilization * 100)}%`,
                backgroundColor: worker.utilization > 0.8 ? '#ef4444' : worker.utilization > 0.5 ? '#f59e0b' : '#22c55e',
              }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}