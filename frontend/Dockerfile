FROM node:20-alpine AS build
WORKDIR /app

RUN corepack enable
COPY pnpm-workspace.yaml package.json pnpm-lock.yaml ./
COPY frontend/package.json frontend/
RUN pnpm install --filter frontend --frozen-lockfile

COPY frontend frontend
RUN pnpm --filter frontend build

FROM nginx:1.27-alpine AS runtime
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/frontend/dist /usr/share/nginx/html
EXPOSE 80
