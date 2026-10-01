import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

api.interceptors.request.use((config) => {
  const correlationId = crypto.randomUUID();
  config.headers['X-Correlation-ID'] = correlationId;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Handle unauthorized
    }
    return Promise.reject(error);
  }
);

export const highLoadApi = {
  getEvents: () => api.get('/api/events'),
  getEvent: (id: string) => api.get(`/api/events/${id}`),
  createEvent: (data: any) => api.post('/api/events', data),
  updateEvent: (id: string, data: any) => api.put(`/api/events/${id}`, data),
  activateEvent: (id: string) => api.post(`/api/events/${id}/activate`),
  deactivateEvent: (id: string) => api.post(`/api/events/${id}/deactivate`),
  getInventory: (eventId: string) => api.get(`/api/events/${eventId}/inventory`),

  getPurchases: (params?: { eventId?: string; userId?: string }) => api.get('/api/purchases', { params }),
  getPurchase: (id: string) => api.get(`/api/purchases/${id}`),
  createPurchase: (data: any) => api.post('/api/purchases', data),
  processPurchase: (id: string) => api.post(`/api/purchases/${id}/process`),
  completePurchase: (id: string) => api.post(`/api/purchases/${id}/complete`),
  failPurchase: (id: string, reason: string) => api.post(`/api/purchases/${id}/fail`, { reason }),
  cancelPurchase: (id: string) => api.post(`/api/purchases/${id}/cancel`),

  getVideoJobs: (params?: { status?: string }) => api.get('/api/videojobs', { params }),
  getVideoJob: (id: string) => api.get(`/api/videojobs/${id}`),
  createVideoJob: (data: any) => api.post('/api/videojobs', data),
  assignJob: (id: string, workerId: string) => api.post(`/api/videojobs/${id}/assign`, { workerId }),
  completeJob: (id: string) => api.post(`/api/videojobs/${id}/complete`),
  failJob: (id: string, error: string) => api.post(`/api/videojobs/${id}/fail`, { error }),
  retryJob: (id: string) => api.post(`/api/videojobs/${id}/retry`),
  cancelJob: (id: string) => api.post(`/api/videojobs/${id}/cancel`),
  getQueueDepth: () => api.get('/api/videojobs/queue-depth'),

  getWorkers: (params?: { type?: string }) => api.get('/api/workers', { params }),
  getWorker: (id: string) => api.get(`/api/workers/${id}`),
  createWorker: (data: any) => api.post('/api/workers', data),
  workerHeartbeat: (id: string) => api.post(`/api/workers/${id}/heartbeat`),
  workerStartJob: (id: string, jobId: string) => api.post(`/api/workers/${id}/start-job`, { jobId }),
  workerCompleteJob: (id: string, success: boolean) => api.post(`/api/workers/${id}/complete-job`, { success }),
  workerFail: (id: string, error: string) => api.post(`/api/workers/${id}/fail`, { error }),
  workerStop: (id: string) => api.post(`/api/workers/${id}/stop`),
};

export const paymentApi = {
  getPayments: (params?: { status?: string }) => api.get('/api/payments', { params }),
  getPayment: (id: string) => api.get(`/api/payments/${id}`),
  createPayment: (data: any) => api.post('/api/payments', data),
  processPayment: (id: string) => api.post(`/api/payments/${id}/process`),
  authorizePayment: (id: string, code: string) => api.post(`/api/payments/${id}/authorize`, { authorizationCode: code }),
  capturePayment: (id: string) => api.post(`/api/payments/${id}/capture`),
  completePayment: (id: string) => api.post(`/api/payments/${id}/complete`),
  failPayment: (id: string, reason: string) => api.post(`/api/payments/${id}/fail`, { reason }),
  refundPayment: (id: string, amount: number) => api.post(`/api/payments/${id}/refund`, { amount }),
  cancelPayment: (id: string) => api.post(`/api/payments/${id}/cancel`),
  retryPayment: (id: string) => api.post(`/api/payments/${id}/retry`),

  getWebhooks: (params?: { paymentId?: string }) => api.get('/api/webhooks', { params }),
  getWebhook: (id: string) => api.get(`/api/webhooks/${id}`),
  sendWebhook: (id: string) => api.post(`/api/webhooks/${id}/send`),
  retryWebhook: (id: string) => api.post(`/api/webhooks/${id}/retry`),
};

export const simulationApi = {
  getMetrics: (scenario: string) => api.get('/api/simulation/metrics', { params: { scenario } }),
  getQueueMetrics: (queueName: string) => api.get('/api/simulation/queue-metrics', { params: { queueName } }),
  getHealth: () => api.get('/api/simulation/health'),
  getSnapshot: () => api.get('/api/simulation/snapshot'),
};

export const failureApi = {
  getFailures: () => api.get('/api/failure-injection'),
  getFailure: (type: string) => api.get(`/api/failure-injection/${type}`),
  injectFailure: (data: { failureType: string; rate: number; durationMs?: number }) => api.post('/api/failure-injection', data),
  clearFailure: (type: string) => api.delete(`/api/failure-injection/${type}`),
  clearAllFailures: () => api.delete('/api/failure-injection'),
};

export const circuitBreakerApi = {
  getAll: () => api.get('/api/circuit-breaker'),
  get: (name: string) => api.get(`/api/circuit-breaker/${name}`),
  configure: (data: { name: string; failureThreshold: number; timeout: string; samplingDuration: number }) => api.post('/api/circuit-breaker', data),
};

export const rateLimitApi = {
  get: (key: string) => api.get(`/api/rate-limit/${key}`),
  configure: (data: { key: string; limit: number; window: string }) => api.post('/api/rate-limit', data),
};