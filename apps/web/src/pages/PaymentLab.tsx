import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { CreditCard, RefreshCw, AlertTriangle, CheckCircle, XCircle, ArrowRightLeft, DollarSign, RotateCcw, ExternalLink, Search, Plus, Settings } from 'lucide-react';
import { clsx } from 'clsx';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { api, paymentApi, simulationApi, failureApi, circuitBreakerApi } from '@/utils/api';
import { Payment, PaymentStatus, Webhook, WebhookEventType } from '@/types/simulation';

const statusConfig = {
  created: { label: 'Created', color: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300', icon: CreditCard },
  processing: { label: 'Processing', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300', icon: RotateCcw },
  authorized: { label: 'Authorized', color: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300', icon: DollarSign },
  captured: { label: 'Captured', color: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300', icon: ArrowRightLeft },
  completed: { label: 'Completed', color: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300', icon: CheckCircle },
  failed: { label: 'Failed', color: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300', icon: XCircle },
  cancelled: { label: 'Cancelled', color: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300', icon: XCircle },
  refunded: { label: 'Refunded', color: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-300', icon: RotateCcw },
};

const webhookEventLabels: Record<WebhookEventType, string> = {
  'payment.created': 'Payment Created',
  'payment.processing': 'Payment Processing',
  'payment.authorized': 'Payment Authorized',
  'payment.completed': 'Payment Completed',
  'payment.failed': 'Payment Failed',
  'payment.refunded': 'Payment Refunded',
};

export default function PaymentLab() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchIdempotencyKey, setSearchIdempotencyKey] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({
    amount: 100,
    currency: 'BRL',
    description: 'Test payment',
    payerEmail: 'user@example.com',
    payerName: 'Test User',
    cardToken: 'tok_test_' + Math.random().toString(36).substr(2, 9),
    idempotencyKey: 'idem_' + Date.now().toString(36),
  });

  const { data: payments, isLoading } = useQuery({
    queryKey: ['payments', statusFilter],
    queryFn: () => paymentApi.getPayments({ status: statusFilter !== 'all' ? statusFilter : undefined }).then(r => r.data),
    refetchInterval: 3000,
  });

  const { data: circuitBreakers } = useQuery({
    queryKey: ['circuit-breakers'],
    queryFn: () => circuitBreakerApi.getAll().then(r => r.data),
    refetchInterval: 5000,
  });

  const { data: rateLimit } = useQuery({
    queryKey: ['rate-limit', 'payment-api'],
    queryFn: () => simulationApi.getSnapshot().then(r => r.data),
    refetchInterval: 5000,
  });

  const createPaymentMutation = useMutation({
    mutationFn: (data: any) => paymentApi.createPayment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      setShowCreateModal(false);
    },
  });

  const processPaymentMutation = useMutation({
    mutationFn: (id: string) => paymentApi.processPayment(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['payments'] }),
  });

  const retryPaymentMutation = useMutation({
    mutationFn: (id: string) => paymentApi.retryPayment(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['payments'] }),
  });

  const refundPaymentMutation = useMutation({
    mutationFn: ({ id, amount }: { id: string; amount: number }) => paymentApi.refundPayment(id, amount),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['payments'] }),
  });

  const sendWebhookMutation = useMutation({
    mutationFn: (id: string) => paymentApi.sendWebhook(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['payments'] }),
  });

  const handleCreatePayment = (e: React.FormEvent) => {
    e.preventDefault();
    createPaymentMutation.mutate(createForm);
  };

  const filteredPayments = payments?.filter(p => 
    !searchIdempotencyKey || p.idempotencyKey.includes(searchIdempotencyKey)
  ) || [];

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Payment Gateway Lab</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">Idempotency, retries, circuit breakers, and webhook delivery</p>
        </div>
        <div className="flex items-center gap-3">
          <Button onClick={() => setShowCreateModal(true)} size="sm">
            <Plus className="w-4 h-4 mr-2" />
            Create Payment
          </Button>
          <Select
            value={statusFilter}
            onValueChange={setStatusFilter}
            options={[
              { value: 'all', label: 'All Statuses' },
              { value: 'created', label: 'Created' },
              { value: 'processing', label: 'Processing' },
              { value: 'authorized', label: 'Authorized' },
              { value: 'captured', label: 'Captured' },
              { value: 'completed', label: 'Completed' },
              { value: 'failed', label: 'Failed' },
              { value: 'cancelled', label: 'Cancelled' },
              { value: 'refunded', label: 'Refunded' },
            ]}
            className="w-48"
          />
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Total Payments</CardTitle>
            <CreditCard className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-gray-900 dark:text-white">{payments?.length || 0}</div>
            <p className="text-xs text-gray-500 dark:text-gray-400">All time</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Success Rate</CardTitle>
            <CheckCircle className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-green-600 dark:text-green-400">
              {payments && payments.length > 0
                ? `${((payments.filter(p => p.status === 'completed').length / payments.length) * 100).toFixed(1)}%`
                : '0%'}
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Completed / Total</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Circuit Breaker</CardTitle>
            <Settings className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-gray-900 dark:text-white">
              {circuitBreakers?.some(c => c.state === 'open') ? 'OPEN' : 'CLOSED'}
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {circuitBreakers?.some(c => c.state === 'open') ? 'Bank circuit open' : 'All circuits closed'}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-500 dark:text-gray-400">Rate Limit</CardTitle>
            <AlertTriangle className="w-5 h-5 text-gray-400" />
          </CardHeader>
          <CardContent>
            <div className="text-3xl font-bold text-gray-900 dark:text-white">
              {rateLimit ? `${rateLimit.requestsPerSecond.toFixed(0)}/s` : 'N/A'}
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Current throughput</p>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="w-5 h-5" />
                Payments
              </CardTitle>
              <div className="flex items-center gap-2">
                <Input
                  placeholder="Search by idempotency key..."
                  value={searchIdempotencyKey}
                  onChange={(e) => setSearchIdempotencyKey(e.target.value)}
                  className="w-64"
                />
                <Button variant="outline" size="sm" onClick={() => queryClient.invalidateQueries({ queryKey: ['payments'] })}>
                  <RefreshCw className="w-4 h-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Reference</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Amount</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Idempotency Key</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Retries</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {filteredPayments.map((payment: Payment) => {
                    const status = statusConfig[payment.status as keyof typeof statusConfig] || statusConfig.created;
                    const StatusIcon = status.icon;
                    return (
                      <tr key={payment.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="p-3">
                          <div>
                            <p className="font-mono text-sm text-gray-900 dark:text-white">{payment.externalReference}</p>
                            <p className="text-xs text-gray-500 dark:text-gray-400">{payment.payerEmail}</p>
                          </div>
                        </td>
                        <td className="p-3 text-sm text-gray-900 dark:text-white">
                          {payment.amount.toFixed(2)} {payment.currency}
                        </td>
                        <td className="p-3">
                          <span className={clsx('inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium', status.color)}>
                            <StatusIcon className="w-3 h-3" />
                            {status.label}
                          </span>
                        </td>
                        <td className="p-3">
                          <code className="text-xs bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded font-mono">
                            {payment.idempotencyKey}
                          </code>
                        </td>
                        <td className="p-3 text-sm text-gray-900 dark:text-white">
                          {payment.retryCount} / {payment.maxRetries}
                        </td>
                        <td className="p-3">
                          <div className="flex items-center gap-1">
                            {payment.status === 'created' || payment.status === 'failed' ? (
                              <Button
                                variant="default"
                                size="sm"
                                onClick={() => processPaymentMutation.mutate(payment.id)}
                                disabled={processPaymentMutation.isPending}
                              >
                                Process
                              </Button>
                            ) : payment.status === 'processing' ? (
                              <Button variant="outline" size="sm" disabled>
                                <RotateCcw className="w-4 h-4 animate-spin" />
                              </Button>
                            ) : payment.status === 'authorized' ? (
                              <Button variant="default" size="sm" onClick={() => paymentApi.capturePayment(payment.id)}>
                                Capture
                              </Button>
                            ) : payment.status === 'captured' ? (
                              <Button variant="default" size="sm" onClick={() => paymentApi.completePayment(payment.id)}>
                                Complete
                              </Button>
                            ) : payment.status === 'failed' && payment.retryCount < payment.maxRetries ? (
                              <Button variant="outline" size="sm" onClick={() => retryPaymentMutation.mutate(payment.id)}>
                                <RotateCcw className="w-4 h-4 mr-1" />
                                Retry
                              </Button>
                            ) : payment.status === 'completed' ? (
                              <Button variant="outline" size="sm" onClick={() => {
                                const amount = parseFloat(prompt('Refund amount:') || '0');
                                if (amount > 0) refundPaymentMutation.mutate({ id: payment.id, amount });
                              }}>
                                Refund
                              </Button>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                  {filteredPayments.length === 0 && (
                    <tr>
                      <td colSpan={6} className="p-8 text-center text-gray-500 dark:text-gray-400">
                        No payments found
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Resilience Controls
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <h4 className="font-medium text-sm mb-2">Circuit Breakers</h4>
              <div className="space-y-2">
                {circuitBreakers?.map((cb: any) => (
                  <div key={cb.name} className="flex items-center justify-between text-sm">
                    <span className="font-medium">{cb.name}</span>
                    <span className={clsx(
                      'px-2 py-1 rounded-full text-xs font-medium',
                      cb.state === 'open' ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400' :
                      cb.state === 'half-open' ? 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400' :
                      'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                    )}>
                      {cb.state.toUpperCase()}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <h4 className="font-medium text-sm mb-2">Failure Injection</h4>
              <FailureInjectionPanel />
            </div>

            <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <h4 className="font-medium text-sm mb-2">Idempotency Test</h4>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                Create two payments with the same idempotency key to test deduplication
              </p>
              <Button
                variant="outline"
                size="sm"
                className="w-full"
                onClick={() => {
                  const key = 'idem_test_' + Date.now();
                  paymentApi.createPayment({ ...createForm, idempotencyKey: key, amount: 50 });
                  setTimeout(() => paymentApi.createPayment({ ...createForm, idempotencyKey: key, amount: 50 }), 100);
                }}
              >
                Test Idempotency
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ExternalLink className="w-5 h-5" />
            Webhooks
          </CardTitle>
        </CardHeader>
        <CardContent>
          {payments && payments.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Event</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Payment</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Attempts</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                    <th className="text-left p-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {payments.flatMap(p => p.webhooks).map((webhook: Webhook) => (
                    <tr key={webhook.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="p-3">
                        <span className="px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300">
                          {webhookEventLabels[webhook.eventType as WebhookEventType] || webhook.eventType}
                        </span>
                      </td>
                      <td className="p-3 text-sm font-mono text-gray-900 dark:text-white">{webhook.paymentId.toString().slice(0, 8)}...</td>
                      <td className="p-3 text-sm text-gray-900 dark:text-white">{webhook.attemptCount} / {webhook.maxAttempts}</td>
                      <td className="p-3">
                        <span className={clsx('px-2 py-1 rounded-full text-xs font-medium', webhook.isDelivered ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300' : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300')}>
                          {webhook.isDelivered ? 'Delivered' : 'Pending'}
                        </span>
                      </td>
                      <td className="p-3">
                        {!webhook.isDelivered && webhook.canRetry && (
                          <Button variant="outline" size="sm" onClick={() => sendWebhookMutation.mutate(webhook.id)}>
                            <RotateCcw className="w-4 h-4 mr-1" />
                            Retry
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {showCreateModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-800 rounded-xl p-6 w-full max-w-md mx-4">
            <h2 className="text-xl font-bold mb-4">Create Payment</h2>
            <form onSubmit={handleCreatePayment} className="space-y-4">
              <Input label="Amount" type="number" step="0.01" value={createForm.amount} onChange={e => setCreateForm({...createForm, amount: parseFloat(e.target.value)})} required />
              <Input label="Currency" value={createForm.currency} onChange={e => setCreateForm({...createForm, currency: e.target.value})} />
              <Input label="Description" value={createForm.description} onChange={e => setCreateForm({...createForm, description: e.target.value})} />
              <Input label="Payer Email" type="email" value={createForm.payerEmail} onChange={e => setCreateForm({...createForm, payerEmail: e.target.value})} required />
              <Input label="Payer Name" value={createForm.payerName} onChange={e => setCreateForm({...createForm, payerName: e.target.value})} required />
              <Input label="Card Token" value={createForm.cardToken} onChange={e => setCreateForm({...createForm, cardToken: e.target.value})} required />
              <Input label="Idempotency Key" value={createForm.idempotencyKey} onChange={e => setCreateForm({...createForm, idempotencyKey: e.target.value})} required />
              <div className="flex gap-2 pt-4">
                <Button type="submit" className="flex-1" disabled={createPaymentMutation.isPending}>
                  {createPaymentMutation.isPending ? 'Creating...' : 'Create Payment'}
                </Button>
                <Button type="button" variant="outline" className="flex-1" onClick={() => setShowCreateModal(false)}>
                  Cancel
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function FailureInjectionPanel() {
  const [failureType, setFailureType] = useState('database-latency');
  const [rate, setRate] = useState(0.1);
  const [duration, setDuration] = useState(5000);

  const injectMutation = useMutation({
    mutationFn: (data: any) => failureApi.injectFailure(data),
  });

  const clearAllMutation = useMutation({
    mutationFn: () => failureApi.clearAllFailures(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['circuit-breakers'] }),
  });

  return (
    <div className="space-y-3">
      <Select
        label="Failure Type"
        value={failureType}
        onValueChange={setFailureType}
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
        <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Rate: {(rate * 100).toFixed(0)}%</label>
        <input type="range" min="0" max="100" step="5" value={rate * 100} onChange={e => setRate(parseInt(e.target.value) / 100)} className="w-full" />
      </div>
      <div>
        <label className="block text-xs text-gray-500 dark:text-gray-400 mb-1">Duration: {duration}ms</label>
        <input type="range" min="1000" max="60000" step="1000" value={duration} onChange={e => setDuration(parseInt(e.target.value))} className="w-full" />
      </div>
      <div className="flex gap-2">
        <Button size="sm" className="flex-1" onClick={() => injectMutation.mutate({ failureType, rate, durationMs: duration })}>
          Inject
        </Button>
        <Button size="sm" variant="outline" className="flex-1" onClick={() => clearAllMutation.mutate()}>
          Clear All
        </Button>
      </div>
    </div>
  );
}