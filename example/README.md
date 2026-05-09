# Example MCP servers (Python)

These apps use the **official MCP Python SDK** (`mcp` on PyPI, v1.x) with **Starlette** composition so each process exposes:

| Path | Purpose |
|------|--------|
| `GET /health` | **Gateway health checks** — `McpAdapterService` calls `{adapterUrl}/health` |
| `GET/POST /mcp` | **Streamable HTTP** transport (default for `FastMCP` + `streamable_http_app`) |
| `GET /sse` + `POST /messages` | **SSE** transport (default for `FastMCP` + `sse_app`) |

Each server listens on its **own port** (different upstream address). Register the **base URL only** in the gateway (no `/mcp` or `/sse` suffix), for example `http://127.0.0.1:9101`.

## Prerequisites

- Python **3.11+** and either **[uv](https://docs.astral.sh/uv/)** or **pip**

### If you see `command not found: uv`

Install uv **as your normal user** (do **not** use `sudo` — the script installs into `~/.local/bin` and `sudo` breaks permissions there):

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

Then open a **new** terminal (or `source ~/.local/bin/env` if the installer added it), ensure `~/.local/bin` is on your `PATH`, and run `cd example && uv sync`.

If a previous `sudo` install left `~/.local` or `~/.local/bin` owned by root, fix ownership once:

```bash
sudo chown -R "$(whoami)" ~/.local
```

Then run the `curl … | sh` line again **without** `sudo`.

Or skip uv and use **pip** (no `uv` required):

```bash
cd example
python3 -m venv .venv
source .venv/bin/activate   # Windows: .venv\Scripts\activate
pip install -r requirements.txt
pip install -e .
```

With pip, run servers with `python -m uvicorn …` (examples below). If `pip install -e .` fails, you can skip it and set **`PYTHONPATH=.`** (current directory must be `example/`) so `import servers` resolves.

## Run all servers at once

From `example/` (after `uv sync` or pip install as above):

```bash
chmod +x run-all-servers.sh   # once
./run-all-servers.sh
```

Uses **`uv run`** when `uv` is on your `PATH`, otherwise **`python3 -m uvicorn`** with `PYTHONPATH` set to this folder (activate your venv first if you use pip). **Ctrl+C** stops every server.

## Run locally (four terminals)

**Using uv:**

```bash
cd example
uv sync
uv run uvicorn servers.streamable_calculator:app --host 0.0.0.0 --port 9101
uv run uvicorn servers.streamable_knowledge:app --host 0.0.0.0 --port 9102
uv run uvicorn servers.sse_taskboard:app --host 0.0.0.0 --port 9103
uv run uvicorn servers.streamable_orders:app --host 0.0.0.0 --port 9104
```

**Using pip** (after `pip install -r requirements.txt` and `pip install -e .` with `.venv` activated):

```bash
cd example
source .venv/bin/activate
python -m uvicorn servers.streamable_calculator:app --host 0.0.0.0 --port 9101
python -m uvicorn servers.streamable_knowledge:app --host 0.0.0.0 --port 9102
python -m uvicorn servers.sse_taskboard:app --host 0.0.0.0 --port 9103
python -m uvicorn servers.streamable_orders:app --host 0.0.0.0 --port 9104
```

Override port with `PORT` if needed (each module reads `os.environ["PORT"]` for the health JSON payload only; you must still pass `--port` to uvicorn).

## Register in MCP Gateway

Create one **adapter** per server. Set **URL** to the base origin and choose the **type** that matches the server.

| Suggested adapter **name** | **URL** | **Type** | Scenario |
|----------------------------|-----------|----------|----------|
| `example-calc` | `http://127.0.0.1:9101` | Streamable HTTP | `add`, `multiply`, `factorial` |
| `example-kb` | `http://127.0.0.1:9102` | Streamable HTTP | `echo`, `word_count`, resource `kb://overview`, prompt `summarize_topic` |
| `example-board` | `http://127.0.0.1:9103` | SSE | `list_columns`, `add_task`, `move_task` |
| `example-orders` | `http://127.0.0.1:9104` | Streamable HTTP | Structured `OrderResult`, `lookup_sku` |

The gateway rewrites paths so client calls to `…/adapters/{name}/mcp` or `…/adapters/{name}/sse` map to the upstream `/mcp` or `/sse` on that base URL.

If the gateway runs **inside Docker** and examples run on the host, use `http://host.docker.internal:9101` (etc.) instead of `127.0.0.1`.

## Docker Compose

From the `example/` directory:

```bash
docker compose up --build
```

This starts all four servers; see `docker-compose.yml` for port mappings.

## MCP Inspector

```bash
cd example
uv sync
# Streamable HTTP example:
npx -y @modelcontextprotocol/inspector
# Then connect to http://127.0.0.1:9101/mcp (or use the inspector’s HTTP transport URL field).
```

## Layout

- `servers/_asgi.py` — shared `GET /health`, CORS for browser clients, `session_manager` lifespan
- `servers/streamable_calculator.py` — numeric tools
- `servers/streamable_knowledge.py` — tools + resource + prompt
- `servers/sse_taskboard.py` — SSE transport
- `servers/streamable_orders.py` — Pydantic-typed tool I/O
