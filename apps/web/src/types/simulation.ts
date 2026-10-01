export type SimulationScenario = 'ticket-sale' | 'streaming' | 'payment-processing' | 'chaos-engineering';

export type QueueProvider = 'redis' | 'kafka';

export type FailureType = 
  | 'database-latency' 
  | 'database-unavailable' 
  | 'redis-unavailable' 
  | 'worker-failure' 
  | 'network-timeout' 
  | 'random-errors';

export interface SimulationConfig {
  scenario: SimulationScenario;
  users: number;
  requestsPerUser: number;
  workers: number;
  queueProvider: QueueProvider;
  enableFailureInjection: boolean;
  failureType?: FailureType;
  failureRate: number;
  failureDurationMs: number;
}

export interface SimulationResult {
  simulationId: string;
  scenario: SimulationScenario;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  queuedRequests: number;
  throughput: number;
  avgLatencyMs: number;
  p50LatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  queueDepth: number;
  pendingJobs: number;
  processingJobs: number;
  completedJobs: number;
  failedJobs: number;
  duration: number;
  workers: WorkerStatus[];
}

export interface WorkerStatus {
  id: string;
  name: string;
  type: string;
  status: 'idle' | 'processing' | 'failed' | 'stopped';
  maxConcurrency: number;
  currentJobs: number;
  totalJobsProcessed: number;
  totalJobsFailed: number;
  utilization: number;
  lastHeartbeat: string | null;
}

export interface QueueMetrics {
  queueName: string;
  pending: number;
  processing: number;
  completed: number;
  failed: number;
  throughput: number;
  avgProcessingTimeMs: number;
}

export interface HealthCheck {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  timestamp: string;
  components: Record<string, string>;
}

export interface MetricsSnapshot {
  timestamp: string;
  requestsPerSecond: number;
  errorRate: number;
  p50LatencyMs: number;
  p95LatencyMs: number;
  p99LatencyMs: number;
  queueDepth: number;
  activeWorkers: number;
  circuitBreakers: CircuitBreakerStatus[];
}

export interface CircuitBreakerStatus {
  name: string;
  state: 'closed' | 'open' | 'half-open';
  failureCount: number;
  successCount: number;
  lastStateChange: string | null;
}

export interface FailureConfig {
  type: FailureType;
  rate: number;
  durationMs?: number;
  injectedAt: string;
}

export interface Event {
  id: string;
  name: string;
  description: string;
  eventDate: string;
  totalCapacity: number;
  availableTickets: number;
  reservedTickets: number;
  soldTickets: number;
  ticketPrice: number;
  currency: string;
  isActive: boolean;
  createdAt: string;
}

export interface Ticket {
  id: string;
  eventId: string;
  userId: string;
  code: string;
  price: number;
  currency: string;
  status: 'available' | 'reserved' | 'sold' | 'cancelled';
  reservedAt?: string;
  soldAt?: string;
  cancelledAt?: string;
}

export interface Purchase {
  id: string;
  eventId: string;
  userId: string;
  quantity: number;
  totalAmount: number;
  currency: string;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled';
  failureReason?: string;
  processedAt?: string;
  completedAt?: string;
  tickets: Ticket[];
}

export interface VideoJob {
  id: string;
  videoId: string;
  videoSizeBytes: number;
  durationSeconds: number;
  operation: string;
  priority: 'low' | 'normal' | 'high' | 'critical';
  status: 'queued' | 'processing' | 'completed' | 'failed' | 'cancelled';
  workerId?: string;
  errorMessage?: string;
  retryCount: number;
  queuedAt?: string;
  startedAt?: string;
  completedAt?: string;
  processingTime?: number;
}

export interface Payment {
  id: string;
  externalReference: string;
  amount: number;
  currency: string;
  description: string;
  payerEmail: string;
  payerName: string;
  status: 'created' | 'processing' | 'authorized' | 'captured' | 'completed' | 'failed' | 'cancelled' | 'refunded';
  failureReason?: string;
  authorizationCode?: string;
  authorizedAt?: string;
  capturedAt?: string;
  completedAt?: string;
  failedAt?: string;
  retryCount: number;
  attempts: PaymentAttempt[];
  webhooks: Webhook[];
}

export interface PaymentAttempt {
  id: string;
  paymentId: string;
  status: 'pending' | 'success' | 'failed' | 'timeout';
  errorMessage?: string;
  authorizationCode?: string;
  duration?: number;
  completedAt?: string;
}

export interface Webhook {
  id: string;
  paymentId: string;
  eventType: 'payment.created' | 'payment.processing' | 'payment.authorized' | 'payment.completed' | 'payment.failed' | 'payment.refunded';
  payload: string;
  attemptCount: number;
  isDelivered: boolean;
  lastError?: string;
  deliveredAt?: string;
  nextRetryAt?: string;
}