import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

export const errorRate = new Rate('errors');
export const requestDuration = new Trend('request_duration');

export const options = {
  stages: [
    { duration: '10s', target: 100 },
    { duration: '10s', target: 5000 },
    { duration: '30s', target: 5000 },
    { duration: '10s', target: 100 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(99)<5000'],
    http_req_failed: ['rate<0.1'],
    errors: ['rate<0.1'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const EVENT_ID = __ENV.EVENT_ID || '';

function getHeaders() {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `spike-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export function setup() {
  const eventPayload = JSON.stringify({
    name: 'Spike Test Event',
    description: 'Event for spike testing',
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

  const idempotencyKey = `spike_${__VU}_${__ITER}`;

  const purchasePayload = JSON.stringify({
    eventId: eventId,
    userId: `user-${__VU}-${__ITER}`,
    quantity: 1,
    idempotencyKey: idempotencyKey,
  });

  const res = http.post(`${BASE_URL}/api/purchases`, purchasePayload, { headers: getHeaders() });
  
  const success = check(res, {
    'request handled': (r) => r.status === 201 || r.status === 409 || r.status === 429,
  });
  
  errorRate.add(!success);
  requestDuration.add(res.timings.duration);

  sleep(0.01);
}