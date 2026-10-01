import { clsx } from 'clsx';
import { Server, CheckCircle, AlertCircle, Loader2, Cpu } from 'lucide-react';

interface Worker {
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
}

const statusConfig = {
  Idle: { icon: Server, color: 'text-gray-500', bg: 'bg-gray-100 dark:bg-gray-800', label: 'Idle' },
  Processing: { icon: Loader2, color: 'text-blue-500', bg: 'bg-blue-100 dark:bg-blue-900/30', label: 'Processing' },
  Failed: { icon: AlertCircle, color: 'text-red-500', bg: 'bg-red-100 dark:bg-red-900/30', label: 'Failed' },
  Stopped: { icon: Server, color: 'text-gray-500', bg: 'bg-gray-100 dark:bg-gray-800', label: 'Stopped' },
};

export function WorkerStatus({ workers }: { workers: Worker[] }) {
  if (workers.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500 dark:text-gray-400">
        <Server className="w-12 h-12 mx-auto mb-2 opacity-50" />
        <p>No workers registered</p>
        <p className="text-sm mt-1">Start a worker to process jobs</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {workers.map((worker) => {
        const config = statusConfig[worker.status as keyof typeof statusConfig] || statusConfig.Idle;
        const StatusIcon = config.icon;
        const isHealthy = worker.lastHeartbeat && new Date(worker.lastHeartbeat).getTime() > Date.now() - 60000;

        return (
          <div key={worker.id} className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-3">
                <div className={clsx('p-2 rounded-lg', config.bg)}>
                  <StatusIcon className={clsx('w-5 h-5', config.color)} />
                </div>
                <div>
                  <p className="font-medium text-gray-900 dark:text-white">{worker.name}</p>
                  <p className="text-sm text-gray-500 dark:text-gray-400">{worker.type} worker</p>
                </div>
              </div>
              <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', config.bg, config.color.replace('text-', 'text-'))}>
                {config.label}
              </span>
            </div>

            <div className="grid grid-cols-4 gap-4 text-sm">
              <div>
                <p className="text-gray-500 dark:text-gray-400">Concurrency</p>
                <p className="font-medium">{worker.currentJobs}/{worker.maxConcurrency}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Utilization</p>
                <p className="font-medium">
                  <span className={clsx(worker.utilization > 0.8 ? 'text-red-500' : worker.utilization > 0.5 ? 'text-yellow-500' : 'text-green-500')}>
                    {(worker.utilization * 100).toFixed(0)}%
                  </span>
                </p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Processed</p>
                <p className="font-medium text-green-600 dark:text-green-400">{worker.totalJobsProcessed}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Failed</p>
                <p className="font-medium text-red-600 dark:text-red-400">{worker.totalJobsFailed}</p>
              </div>
            </div>

            <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
              <span>
                {isHealthy ? (
                  <>
                    <CheckCircle className="w-3 h-3 inline text-green-500" />
                    Healthy
                  </>
                ) : (
                  <>
                    <AlertCircle className="w-3 h-3 inline text-red-500" />
                    No heartbeat
                  </>
                )}
              </span>
              <span>Last seen: {worker.lastHeartbeat ? new Date(worker.lastHeartbeat).toLocaleTimeString() : 'Never'}</span>
            </div>
          </div>
        );
      })}
    </div>
  );
}