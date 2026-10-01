import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const requestDuration = new Trend('request_duration');
export const paymentsCreated = new Counter('payments_created');
export const paymentsCompleted = new Counter('payments_completed');
export const paymentsFailed = new Counter('payments_failed');

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '2m', target: 200 },
    { duration: '2m', target: 200 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
    errors: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `payment-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export default function () {
  const idempotencyKey = `payment_${__VU}_${__ITER}_${Date.now()}`;

  // Create payment
  const paymentPayload = JSON.stringify({
    amount: Math.floor(Math.random() * 1000) + 10,
    currency: 'BRL',
    description: `Load test payment ${__VU}-${__ITER}`,
    payerEmail: `user${__VU}@example.com`,
    payerName: `User ${__VU}`,
    cardToken: 'tok_visa',
    idempotencyKey: idempotencyKey,
  });

  const createRes = http.post(`${BASE_URL}/api/payments`, paymentPayload, { headers: getHeaders() });
  
  const createSuccess = check(createRes, {
    'payment created': (r) => r.status === 201 || r.status === 409,
  });
  
  errorRate.add(!createSuccess);
  requestDuration.add(createRes.timings.duration);

  if (createRes.status === 201) {
    paymentsCreated.add(1);
    const paymentId = createRes.json();

    // Process payment
    sleep(0.2);
    const processRes = http.post(`${BASE_URL}/api/payments/${paymentId}/process`, null, { headers: getHeaders() });
    
    const processSuccess = check(processRes, {
      'payment processed': (r) => r.status === 204 || r.status === 503,
    });
    
    errorRate.add(!processSuccess);
    requestDuration.add(processRes.timings.duration);

    if (processRes.status === 204) {
      // Complete payment
      sleep(0.2);
      const completeRes = http.post(`${BASE_URL}/api/payments/${paymentId}/complete`, null, { headers: getHeaders() });
      
      const completeSuccess = check(completeRes, {
        'payment completed': (r) => r.status === 204,
      });
      
      if (completeSuccess) {
        paymentsCompleted.add(1);
      } else {
        paymentsFailed.add(1);
      }
      
      errorRate.add(!completeSuccess);
      requestDuration.add(completeRes.timings.duration);
    } else if (processRes.status === 503) {
      // Circuit breaker open or bank unavailable - expected under load
      paymentsFailed.add(1);
    }
  } else if (createRes.status === 409) {
    // Idempotency key conflict - expected
    paymentsCreated.add(1);
  }

  sleep(Math.random() * 1 + 0.5);
}