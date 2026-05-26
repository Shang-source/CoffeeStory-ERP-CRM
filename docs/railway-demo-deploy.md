# Railway Demo Deployment

This is the temporary demo path for StoryCoffee. It runs the React frontend and ASP.NET API in one Railway web service, backed by Railway PostgreSQL. It does not replace the AWS/VPS production runbooks.

## Why single service

The frontend currently calls relative `/api` URLs. A single Railway service keeps the browser, API, PDF downloads, and auth cookies/tokens on one public origin and avoids CORS/proxy complexity.

Railway supports monorepos via service root/config settings and custom Dockerfile paths. This repo uses root-level `railway.json` and `Dockerfile.railway`.

## Railway services

Create:

1. One PostgreSQL database service.
2. One web service from this GitHub repo.
3. Optional but recommended: one volume attached to the web service at `/var/storycoffee-files`.

Do not create a separate frontend service for the first demo unless you also change the frontend API base URL/proxy strategy.

## Web service settings

- Source repo: `Shang-source/CoffeeStory-ERP-CRM`
- Root directory: `/`
- Config file: `railway.json`
- Dockerfile: `Dockerfile.railway`
- Healthcheck path: `/ready`

`railway.json` already sets the Dockerfile path and healthcheck.

## Cloudflare domain

Use Cloudflare as the DNS provider and point the domain to the Railway web service.

Recommended demo domain:

- App URL: `app.yourdomain.com`
- Optional root redirect: `yourdomain.com` -> `app.yourdomain.com`

Railway setup:

1. Open the Railway web service.
2. Go to `Settings` -> `Networking` -> `Custom Domain`.
3. Add `app.yourdomain.com`.
4. Copy the `CNAME` and any `TXT` verification record Railway provides.

Cloudflare DNS setup for `app.yourdomain.com`:

| Type | Name | Target / Content | Proxy status |
| --- | --- | --- | --- |
| `CNAME` | `app` | Railway-provided CNAME target | Proxied / orange cloud |
| `TXT` | Railway-provided name | Railway-provided value | DNS only |

Cloudflare SSL setup:

- `SSL/TLS` -> `Overview` -> set mode to `Full`.
- `SSL/TLS` -> `Edge Certificates` -> keep `Universal SSL` enabled.

After DNS verifies in Railway, update the Railway web service variables:

```env
Portal__BaseUrl=https://app.yourdomain.com
Cors__AllowedOrigins__0=https://app.yourdomain.com
```

For the root domain, keep the Railway demo simple:

1. Add `yourdomain.com` as another Railway custom domain only if your Railway plan allows another custom domain.
2. In Cloudflare, create a root `CNAME` record:
   - `Name` -> `@`
   - `Target` -> Railway-provided CNAME target
   - `Proxy status` -> Proxied / orange cloud
3. Add Railway's `TXT` verification record.

If you only have one Railway custom domain available, use `app.yourdomain.com` for the app and add a Cloudflare redirect rule from `yourdomain.com` to `https://app.yourdomain.com`.

Avoid nested demo domains such as `demo.app.yourdomain.com` unless Cloudflare proxying is disabled or you have Cloudflare Advanced Certificate Manager. A first-level subdomain like `app.yourdomain.com` is the lowest-friction option.

## Variables

Copy `.env.railway.example` into the Railway web service variables and replace placeholders.

Minimum required values:

```env
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
Jwt__Secret=<strong random secret>
DocumentStorage__SigningSecret=<strong random secret>
Portal__BaseUrl=https://<your-web-service>.up.railway.app
Cors__AllowedOrigins__0=https://<your-web-service>.up.railway.app
SeedData__Enabled=true
Email__Provider=Stub
```

If the Railway Postgres service is not named `Postgres`, update all `${{Postgres.*}}` references to match the actual service name.

Generate secrets locally:

```bash
openssl rand -base64 48
```

## Document PDFs

For demo reliability, attach a Railway volume to the web service:

- Mount path: `/var/storycoffee-files`
- Variable: `DocumentStorage__LocalRoot=/var/storycoffee-files`

Without the volume, generated invoice/statement PDFs can disappear after redeploys or restarts because container filesystem storage is ephemeral.

## Email for demo

Default:

```env
Email__Provider=Stub
```

This marks the email workflow as sent without sending real email. For a customer-facing demo with real messages, switch to SMTP:

```env
Email__Provider=Smtp
Email__SmtpHost=<smtp host>
Email__SmtpPort=587
Email__SmtpUsername=<smtp username>
Email__SmtpPassword=<smtp password>
Email__UseStartTls=true
Email__FromAddress=<verified sender>
```

## Deploy checklist

1. Push the repo to GitHub.
2. Create Railway project from GitHub repo.
3. Add PostgreSQL service.
4. Add web service from the repo.
5. Add variables from `.env.railway.example`.
6. Add volume at `/var/storycoffee-files`.
7. Generate Railway public domain.
8. Replace `Portal__BaseUrl` and `Cors__AllowedOrigins__0` with that domain.
9. Redeploy the web service.
10. Open `https://<your-domain>/ready`; expect DB/document storage ready.
11. Open `https://<your-domain>/`.

## Demo accounts

Seed data is enabled for the Railway demo:

- Admin: `admin@storycoffee.co.nz` / `password`
- Customer: `john@aucklandcafe.co.nz` / `password`
- Customer: `sarah@wellingtoncoffee.co.nz` / `password`

Turn `SeedData__Enabled=false` before importing real customer data.

## Validation after deploy

```bash
curl https://<your-domain>/ready
curl https://<your-domain>/health
```

Manual flow:

1. Login as Admin.
2. Open Orders.
3. Send selected orders to production.
4. Complete production.
5. Ship selected + send invoices.
6. Download invoice PDF.
7. Login as Customer and verify orders/invoices.

## Known demo limitations

- Email defaults to `Stub`, so no real external inbox receives messages.
- Local document storage needs the Railway volume to survive redeploys.
- Redis is disabled for the demo to keep the Railway setup cheaper and simpler.
- This is not the final AWS production topology.
