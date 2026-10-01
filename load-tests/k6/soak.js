import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const requestDuration = new Trend('request_duration');
export const requestsTotal = new Counter('requests_total');

export const options = {
  stages: [
    { duration: '5m', target: 200 },
    { duration: '30m', target: 200 },
    { duration: '5m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
  },
  ext: {
    loadimpact: {
      distribution: { 'amazon:us:ashburn': { loadZone: 'amazon:us:ashburn', percent: 100 } },
    },
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EVENT_ID = __ENV.EVENT_ID || '';

function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `soak-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export function setup() {
  const eventPayload = JSON.stringify({
    name: 'Soak Test Event',
    description: 'Event for soak testing',
    eventDate: new Date(Date.now() + 86400000).toISOString(),
    totalCapacity: 1000000,
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

  const idempotencyKey = `soak_${__VU}_${__ITER}`;

  const purchasePayload = JSON.stringify({
    eventId: eventId,
    userId: `user-${__VU}-${__ITER}`,
    quantity: 1,
    idempotencyKey: idempotencyKey,
  });

  const res = http.post(`${BASE_URL}/api/purchases`, purchasePayload, { headers: getHeaders() });
  
  const success = check(res, {
    'purchase created': (r) => r.status === 201 || r.status === 409,
  });
  
  errorRate.add(!success);
  requestDuration.add(res.timings.duration);
  requestsTotal.add(1);

  if (res.status === 201) {
    const purchaseId = res.json();
    
    sleep(0.5);
    const processRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/process`, null, { headers: getHeaders() });
    check(processRes, { 'processed': (r) => r.status === 204 });
    
    if (processRes.status === 204) {
      sleep(0.5);
      const completeRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/complete`, null, { headers: getHeaders() });
      check(completeRes, { 'completed': (r) => r.status === 204 });
    }
  }

  sleep(Math.random() * 2 + 1);
}