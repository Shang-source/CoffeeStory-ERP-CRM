# StoryCoffee Frontend Architecture

StoryCoffee frontend uses a feature-sliced structure under `frontend/src`:

- `app`: bootstrap only, including providers, router, layouts, and guards.
- `pages`: route-level composition for admin, customer, auth, and not-found pages.
- `widgets`: page-sized reusable sections reserved for tables, dashboards, and boards.
- `features`: user workflows and mutations such as auth, order workflow, price book, invoice actions, and standing-order editing.
- `entities`: stable business concepts, typed API facades, and model exports.
- `shared`: reusable infrastructure, OpenAPI generated schema, HTTP/session handling, status formatting, common UI, hooks, config, and utilities.

Public route URLs and backend API contracts are preserved. Figma-generated runtime source and shadcn/Radix dependencies have been removed; archived source documents live in `docs/archive/figma-imports`.
