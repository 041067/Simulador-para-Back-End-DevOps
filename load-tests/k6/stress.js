import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const requestDuration = new Trend('request_duration');

export const options = {
  stages: [
    { duration: '1m', target: 100 },
    { duration: '2m', target: 500 },
    { duration: '3m', target: 500 },
    { duration: '2m', target: 1000 },
    { duration: '3m', target: 1000 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    http_req_failed: ['rate<0.05'],
    errors: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EVENT_ID = __ENV.EVENT_ID || '';

function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `loadtest-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export function setup() {
  const eventPayload = JSON.stringify({
    name: 'Load Test Event',
    description: 'Event for load testing',
    eventDate: new Date(Date.now() + 86400000).toISOString(),
    totalCapacity: 500000,
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

  const idempotencyKey = `loadtest_${__VU}_${__ITER}`;

  const purchasePayload = JSON.stringify({
    eventId: eventId,
    userId: `user-${__VU}-${__ITER}`,
    quantity: Math.floor(Math.random() * 4) + 1,
    idempotencyKey: idempotencyKey,
  });

  const res = http.post(`${BASE_URL}/api/purchases`, purchasePayload, { headers: getHeaders() });
  
  const success = check(res, {
    'purchase created': (r) => r.status === 201 || r.status === 409,
  });
  
  errorRate.add(!success);
  requestDuration.add(res.timings.duration);

  if (res.status === 201) {
    const purchaseId = res.json();
    
    // Try to process
    sleep(0.05);
    const processRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/process`, null, { headers: getHeaders() });
    check(processRes, { 'processed': (r) => r.status === 204 });
    
    if (processRes.status === 204) {
      sleep(0.05);
      const completeRes = http.post(`${BASE_URL}/api/purchases/${purchaseId}/complete`, null, { headers: getHeaders() });
      check(completeRes, { 'completed': (r) => r.status === 204 });
    }
  }

  sleep(Math.random() * 0.3);
}