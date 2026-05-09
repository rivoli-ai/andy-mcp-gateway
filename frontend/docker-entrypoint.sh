#!/bin/sh
# Runtime config for Angular (same idea as andy-devpilot; path matches APP_CONFIG loader).
mkdir -p /usr/share/nginx/html/assets/config
cat > /usr/share/nginx/html/assets/config/config.json <<EOF
{
  "apiUrl": "${API_URL:-http://localhost:8000}"
}
EOF

exec nginx -g "daemon off;"
