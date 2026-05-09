"""
SSE MCP — classic GET ``/sse`` + POST ``/messages`` flow (superseded by streamable HTTP in spec, still supported).

Gateway registration
--------------------
- **Adapter URL**: ``http://127.0.0.1:9103``
- **Type**: SSE (Server-Sent Events)

Run::

    cd example && uv run uvicorn servers.sse_taskboard:app --host 0.0.0.0 --port 9103
"""

from __future__ import annotations

import os
from datetime import UTC, datetime

from mcp.server.fastmcp import FastMCP

from servers._asgi import build_sse_app

PORT = int(os.environ.get("PORT", "9103"))

mcp = FastMCP("Example Task Board")


@mcp.tool()
def list_columns() -> list[str]:
    """Return Kanban-style column names."""
    return ["todo", "in_progress", "done"]


@mcp.tool()
def add_task(title: str, column: str = "todo") -> str:
    """Pretend to add a task; returns a confirmation string."""
    ts = datetime.now(UTC).isoformat()
    return f"Task {title!r} queued in {column!r} at {ts}"


@mcp.tool()
def move_task(task_id: str, to_column: str) -> str:
    """Pretend to move a task between columns."""
    return f"Moved task {task_id!r} -> {to_column!r}"


app = build_sse_app(mcp=mcp, service_id="sse-taskboard", port=PORT)
