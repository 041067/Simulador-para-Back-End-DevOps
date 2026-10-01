import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, Zap, Database, Server, Wifi, Shuffle, X, CheckCircle, Loader2 } from 'lucide-react';
import { clsx } from 'clsx';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { api, failureApi, circuitBreakerApi } from '@/utils/api';
import { FailureType, FailureConfig } from '@/types/simulation';

const failureTypes: { value: FailureType; label: string; icon: any; description: string; color: string }[] = [
  { value: 'database-latency', label: 'Database Latency', icon: Database, description: 'Add artificial latency to database queries', color: 'blue' },
  { value: 'database-unavailable', label: 'Database Unavailable', icon: Database, description: 'Simulate database connection failures', color: 'red' },
  { value: 'redis-unavailable', label: 'Redis Unavailable', icon: Server, description: 'Simulate Redis connection failures', color: 'orange' },
  { value: 'worker-failure', label: 'Worker Failure', icon: Zap, description: 'Simulate worker process crashes', color: 'purple' },
  { value: 'network-timeout', label: 'Network Timeout', icon: Wifi, description: 'Simulate network timeouts and delays', color: 'yellow' },
  { value: 'random-errors', label: 'Random Errors', icon: Shuffle, description: 'Inject random errors at specified rate', color: 'gray' },
];

const colorMap = {
  blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300 border-blue-200 dark:border-blue-800',
  red: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 border-red-200 dark:border-red-800',
  orange: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300 border-orange-200 dark:border-orange-800',
  purple: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300 border-purple-200 dark:border-purple-800',
  yellow: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300 border-yellow-200 dark:border-yellow-800',
  gray: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300 border-gray-200 dark:border-gray-700',
};

export default function FailureInjection() {
  const queryClient = useQueryClient();
  const [selectedType, setSelectedType] = useState<FailureType>('database-latency');
  const [rate, setRate] = useState(0.1);
  const [duration, setDuration] = useState(5000);

  const { data: activeFailures } = useQuery({
    queryKey: ['failures'],
    queryFn: () => failureApi.getFailures().then(r => r.data),
    refetchInterval: 3000,
  });

  const { data: circuitBreakers } = useQuery({
    queryKey: ['circuit-breakers'],
    queryFn: () => circuitBreakerApi.getAll().then(r => r.data),
    refetchInterval: 5000,
  });

  const injectMutation = useMutation({
    mutationFn: (data: { failureType: FailureType; rate: number; durationMs?: number }) => failureApi.injectFailure(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['failures'] });
      queryClient.invalidateQueries({ queryKey: ['circuit-breakers'] });
    },
  });

  const clearMutation = useMutation({
    mutationFn: (type: FailureType) => failureApi.clearFailure(type),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['failures'] }),
  });

  const clearAllMutation = useMutation({
    mutationFn: () => failureApi.clearAllFailures(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['failures'] }),
  });

  const selectedConfig = failureTypes.find(f => f.value === selectedType);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Chaos Engineering</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Inject controlled failures to test system resilience</p>
        </div>
        <Button variant="destructive" onClick={() => clearAllMutation.mutate()} disabled={clearAllMutation.isPending}>
          <X className="w-4 h-4 mr-2" />
          Clear All Failures
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-red-500" />
              Inject Failure
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Failure Type</label>
              <div className="grid grid-cols-2 gap-2">
                {failureTypes.map((f) => (
                  <button
                    key={f.value}
                    onClick={() => setSelectedType(f.value)}
                    className={clsx(
                      'p-4 rounded-lg border-2 text-left transition-all',
                      selectedType === f.value
                        ? `border-${f.color}-500 bg-${f.color}-50 dark:bg-${f.color}-900/20`
                        : 'border-gray-200 dark:border-gray-700 hover:border-purple-300'
                    )}
                  >
                    <f.icon className={clsx('w-6 h-6 mb-2', `text-${f.color}-500`)} />
                    <p className="font-medium text-sm">{f.label}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{f.description}</p>
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-4 pt-4 border-t border-gray-200 dark:border-gray-700">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Failure Rate: {(rate * 100).toFixed(0)}%
                </label>
                <input
                  type="range"
                  min="0"
                  max="100"
                  step="5"
                  value={rate * 100}
                  onChange={(e) => setRate(parseInt(e.target.value) / 100)}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-red-600"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Duration: {duration}ms
                </label>
                <input
                  type="range"
                  min="1000"
                  max="120000"
                  step="1000"
                  value={duration}
                  onChange={(e) => setDuration(parseInt(e.target.value))}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-red-600"
                />
              </div>

              <Button
                className="w-full"
                size="lg"
                onClick={() => injectMutation.mutate({ failureType: selectedType, rate, durationMs: duration })}
                disabled={injectMutation.isPending}
              >
                {injectMutation.isPending ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Injecting...
                  </>
                ) : (
                  <>
                    <Zap className="w-4 h-4 mr-2" />
                    Inject Failure
                  </>
                )}
              </Button>
            </div>
          </CardContent>
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5" />
              Active Failures
            </CardTitle>
          </CardHeader>
          <CardContent>
            {activeFailures && activeFailures.length > 0 ? (
              <div className="space-y-3">
                {activeFailures.map((failure: FailureConfig) => {
                  const config = failureTypes.find(f => f.value === failure.type);
                  const ConfigIcon = config?.icon || AlertTriangle;
                  const elapsed = Date.now() - new Date(failure.injectedAt).getTime();
                  const remaining = failure.durationMs ? Math.max(0, failure.durationMs - elapsed) : null;
                  const progress = failure.durationMs ? Math.min(100, (elapsed / failure.durationMs) * 100) : 0;

                  return (
                    <div key={failure.type} className={clsx('p-4 rounded-lg border', colorMap[config?.color as keyof typeof colorMap] || colorMap.gray)}>
                      <div className="flex items-start justify-between">
                        <div className="flex items-center gap-3">
                          <ConfigIcon className={clsx('w-6 h-6', `text-${config?.color}-500`)} />
                          <div>
                            <p className="font-medium text-gray-900 dark:text-white">{config?.label || failure.type}</p>
                            <p className="text-sm text-gray-500 dark:text-gray-400">
                              Rate: {(failure.rate * 100).toFixed(0)}% • Injected: {new Date(failure.injectedAt).toLocaleTimeString()}
                            </p>
                          </div>
                        </div>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => clearMutation.mutate(failure.type)}
                        >
                          <X className="w-4 h-4" />
                        </Button>
                      </div>

                      {failure.durationMs && (
                        <div className="mt-3">
                          <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mb-1">
                            <span>Time Remaining: {remaining ? `${(remaining / 1000).toFixed(1)}s` : 'Expired'}</span>
                            <span>{progress.toFixed(0)}%</span>
                          </div>
                          <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                            <div
                              className="h-full rounded-full transition-all duration-300"
                              style={{
                                width: `${progress}%`,
                                backgroundColor: progress >= 100 ? '#ef4444' : config?.color || '#6b7280',
                              }}
                            />
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="text-center py-12 text-gray-500 dark:text-gray-400">
                <AlertTriangle className="w-12 h-12 mx-auto mb-4 opacity-50" />
                <p>No active failures injected</p>
                <p className="text-sm mt-2">Select a failure type and click "Inject Failure" to start chaos testing</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Server className="w-5 h-5" />
              Circuit Breaker Status
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {circuitBreakers?.map((cb: any) => (
                <div key={cb.name} className="p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className={clsx('p-2 rounded-lg', cb.state === 'open' ? 'bg-red-100 dark:bg-red-900/30' : cb.state === 'half-open' ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-green-100 dark:bg-green-900/30')}>
                        <Zap className={clsx('w-5 h-5', cb.state === 'open' ? 'text-red-500' : cb.state === 'half-open' ? 'text-yellow-500' : 'text-green-500')} />
                      </div>
                      <div>
                        <p className="font-medium text-gray-900 dark:text-white">{cb.name}</p>
                        <p className="text-sm text-gray-500 dark:text-gray-400">
                          Failures: {cb.failureCount} • Successes: {cb.successCount}
                        </p>
                      </div>
                    </div>
                    <span className={clsx('px-3 py-1 rounded-full text-sm font-medium', cb.state === 'open' ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400' : cb.state === 'half-open' ? 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400' : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400')}>
                      {cb.state.toUpperCase()}
                    </span>
                  </div>
                  {cb.lastStateChange && (
                    <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                      Last state change: {new Date(cb.lastStateChange).toLocaleString()}
                    </p>
                  )}
                </div>
              ))}
              {circuitBreakers?.length === 0 && (
                <p className="text-center text-gray-500 dark:text-gray-400 py-8">No circuit breakers configured</p>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CheckCircle className="w-5 h-5 text-green-500" />
              Resilience Patterns Demo
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <ResilienceCard
                title="Retry with Exponential Backoff"
                description="Automatically retries failed operations with increasing delays"
                pattern="Retry"
                color="blue"
              />
              <ResilienceCard
                title="Circuit Breaker"
                description="Prevents cascading failures by stopping calls to failing services"
                pattern="Circuit Breaker"
                color="red"
              />
              <ResilienceCard
                title="Rate Limiting"
                description="Protects services from overload by limiting request rate"
                pattern="Rate Limit"
                color="yellow"
              />
              <ResilienceCard
                title="Idempotency Keys"
                description="Ensures duplicate requests don't cause double processing"
                pattern="Idempotency"
                color="green"
              />
              <ResilienceCard
                title="Bulkhead Isolation"
                description="Isolates critical resources to prevent resource exhaustion"
                pattern="Bulkhead"
                color="purple"
              />
              <ResilienceCard
                title="Fallback Responses"
                description="Provides graceful degradation when services are unavailable"
                pattern="Fallback"
                color="orange"
              />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function ResilienceCard({ title, description, pattern, color }: { title: string; description: string; pattern: string; color: string }) {
  const colorMap = {
    blue: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    red: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300',
    yellow: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300',
    green: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300',
    purple: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300',
    orange: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300',
  };

  return (
    <div className={clsx('p-4 rounded-lg border', colorMap[color as keyof typeof colorMap] || colorMap.blue, 'dark:border-gray-700')}>
      <div className="flex items-center justify-between mb-2">
        <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', colorMap[color as keyof typeof colorMap] || colorMap.blue)}>
          {pattern}
        </span>
        <CheckCircle className="w-5 h-5 text-green-500" />
      </div>
      <h4 className="font-medium text-gray-900 dark:text-white mb-1">{title}</h4>
      <p className="text-sm text-gray-500 dark:text-gray-400">{description}</p>
    </div>
  );
}