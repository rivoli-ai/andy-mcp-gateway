#!/usr/bin/env bash
# Start all example MCP servers (ports 9101–9104). Ctrl+C stops every child.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

PIDS=()

cleanup() {
  echo ""
  echo "Stopping servers..."
  local i
  for ((i = 0; i < ${#PIDS[@]}; i++)); do
    local pid="${PIDS[i]}"
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
    fi
  done
  echo "Done."
}

trap cleanup EXIT INT TERM

start_server() {
  local label=$1 module=$2 port=$3

  local pid
  if command -v uv >/dev/null 2>&1; then
    uv run uvicorn "$module" --host 0.0.0.0 --port "$port" &
  else
    PYTHONPATH="$ROOT${PYTHONPATH:+:$PYTHONPATH}" python3 -m uvicorn "$module" --host 0.0.0.0 --port "$port" &
  fi
  pid=$!
  PIDS+=("$pid")
  echo "[$label] http://0.0.0.0:$port/health (PID $pid)"
}

echo "Starting four MCP example servers from: $ROOT"
echo "(Uses uv if available, otherwise python3 -m uvicorn with PYTHONPATH=$ROOT)"
echo ""

start_server "calculator " "servers.streamable_calculator:app" 9101
start_server "knowledge  " "servers.streamable_knowledge:app" 9102
start_server "sse board  " "servers.sse_taskboard:app"         9103
start_server "orders     " "servers.streamable_orders:app"    9104

echo ""
echo "All servers running. Press Ctrl+C to stop."
wait
