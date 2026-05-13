import http from 'k6/http';
import { check, group, sleep } from 'k6';

export const options = {
  scenarios: {
    readonly_api_smoke: {
      executor: 'constant-vus',
      vus: Number(__ENV.VUS) || 2,
      duration: __ENV.DURATION || '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1000'],
    checks: ['rate>0.98'],
  },
};

const baseUrl = (__ENV.API_BASE_URL || 'http://localhost:5080').replace(/\/$/, '');
const adminEmail = __ENV.ADMIN_EMAIL || 'admin@storycoffee.co.nz';
const customerEmail = __ENV.CUSTOMER_EMAIL || 'john@aucklandcafe.co.nz';
const password = __ENV.PASSWORD || 'password';

export function setup() {
  return {
    adminToken: login(adminEmail, 'admin'),
    customerToken: login(customerEmail, 'customer'),
  };
}

function login(email, label) {
  const response = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: `${label}_login` } },
  );
  let token = '';
  if (response.status === 200) {
    try {
      token = response.json('accessToken') || '';
    } catch (_) {
      token = '';
    }
  }

  check(response, {
    [`${label} login is 200`]: (result) => result.status === 200,
    [`${label} token returned`]: () => Boolean(token),
  });

  return token;
}

export default function (tokens) {
  group('platform readiness', () => {
    expectOk(http.get(`${baseUrl}/health`, { tags: { endpoint: 'health' } }), 'health');
    expectOk(http.get(`${baseUrl}/ready`, { tags: { endpoint: 'ready' } }), 'ready');
  });

  group('admin read APIs', () => {
    expectOk(get('/api/admin/dashboard', tokens.adminToken, 'admin_dashboard'), 'admin dashboard');
    expectOk(get('/api/admin/orders', tokens.adminToken, 'admin_orders'), 'admin orders');
    expectOk(get('/api/admin/invoices', tokens.adminToken, 'admin_invoices'), 'admin invoices');
    expectOk(get('/api/admin/production/current', tokens.adminToken, 'admin_production_current'), 'admin production current');
    expectOk(get('/api/admin/production/batches', tokens.adminToken, 'admin_production_batches'), 'admin production batches');
    expectOk(get('/api/admin/statements', tokens.adminToken, 'admin_statements'), 'admin statements');
    expectOk(get('/api/admin/logs/audit', tokens.adminToken, 'admin_audit_logs'), 'admin audit logs');
  });

  group('customer read APIs', () => {
    expectOk(get('/api/customer/dashboard', tokens.customerToken, 'customer_dashboard'), 'customer dashboard');
    expectOk(get('/api/customer/orders', tokens.customerToken, 'customer_orders'), 'customer orders');
    expectOk(get('/api/customer/invoices', tokens.customerToken, 'customer_invoices'), 'customer invoices');
    expectOk(get('/api/customer/statements', tokens.customerToken, 'customer_statements'), 'customer statements');
    expectOk(get('/api/customer/products', tokens.customerToken, 'customer_products'), 'customer products');
    expectOk(get('/api/customer/profile', tokens.customerToken, 'customer_profile'), 'customer profile');
  });

  sleep(1);
}

function get(path, token, endpoint) {
  return http.get(`${baseUrl}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
    tags: { endpoint },
  });
}

function expectOk(response, label) {
  check(response, {
    [`${label} is 2xx`]: (result) => result.status >= 200 && result.status < 300,
  });
}
