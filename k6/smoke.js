import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 2,
  duration: '30s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  },
};

const baseUrl = __ENV.API_BASE_URL || 'http://localhost:5080';

export default function () {
  const health = http.get(`${baseUrl}/health`);
  check(health, {
    'health is 200': (response) => response.status === 200,
  });
  sleep(1);
}
