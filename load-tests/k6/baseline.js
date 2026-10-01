import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const requestDuration = new Trend('request_duration');
export const requestsTotal = new Counter('requests_total');

export const options = {
  stages: [
    { duration: '30s', target: 100 },
    { duration: '1m', target: 100 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EVENT_ID = __ENV.EVENT_ID || '';
const IDEMPOTENCY_KEY_PREFIX = 'loadtest_';

function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `loadtest-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export function setup() {
  // Create an event for testing
  const eventPayload = JSON.stringify({
    name: 'Load Test Event',
    description: 'Event for load testing',
    eventDate: new Date(Date.now() + 86400000).toISOString(),
    totalCapacity: 100000,
    ticketPrice: 100,
    currency: 'BRL',
  });

  const res = http.post(`${BASE_URL}/api/events`, eventPayload, { headers: getHeaders() });
  check(res, { 'event created': (r) => r.status === 201 });
  
  const event = res.json();
  return { eventId: event };
}

export default function (data) {
  const eventId = data.eventId || EVENT_ID;
  if (!eventId) return;

  const idempotencyKey = `${IDEMPOTENCY_KEY_PREFIX}${__VU}-${__ITER}`;

  // Test 1: Create purchase
  const purchasePayload = JSON.stringify({
    eventId: eventId,
    userId: `user-${__VU}-${__ITER}`,
    quantity: 1,
    idempotencyKey: idempotencyKey,
  });

  const purchaseRes = http.post(`${BASE_URL}/api/purchases`, purchasePayload, { headers: getHeaders() });
  
  const purchaseSuccess = check(purchaseRes, {
    'purchase created': (r) => r.status === 201 || r.status === 409,
  });
  
  errorRate.add(!purchaseSuccess);
  requestDuration.add(purchaseRes.timings.duration);
  requestsTotal.add(1);

  if (purchaseRes.status === 201) {
    const purchase = purchaseRes.json();
    const purchaseId = purchase;

    // Test 2: Process purchase
    sleep(0.1);
    const processRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/process`, null, { headers: getHeaders() });
    
    const processSuccess = check(processRes, {
      'purchase processed': (r) => r.status === 204,
    });
    
    errorRate.add(!processSuccess);
    requestDuration.add(processRes.timings.duration);
    requestsTotal.add(1);

    if (processRes.status === 204) {
      // Test 3: Complete purchase
      sleep(0.1);
      const completeRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/complete`, null, { headers: getHeaders() });
      
      const completeSuccess = check(completeRes, {
        'purchase completed': (r) => r.status === 204,
      });
      
      errorRate.add(!completeSuccess);
      requestDuration.add(completeRes.timings.duration);
      requestsTotal.add(1);
    }
  }

  sleep(Math.random() * 0.5);
}

export function teardown(data) {
  // Cleanup if needed
}