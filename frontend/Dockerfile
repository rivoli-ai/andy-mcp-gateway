FROM node:20-alpine AS build
WORKDIR /app

ENV NODE_TLS_REJECT_UNAUTHORIZED=0 \
    NPM_CONFIG_STRICT_SSL=false \
    npm_config_strict-ssl=false

COPY package*.json ./
# Prefer npm ci when lockfile is strictly in sync; npm install tolerates transitive drift (same pattern as many Angular Docker builds).
RUN npm config set strict-ssl false && npm install --no-audit --no-fund

COPY . .
RUN npm config set strict-ssl false && npm run build -- --configuration production

FROM nginx:alpine
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80

CMD ["/docker-entrypoint.sh"]
