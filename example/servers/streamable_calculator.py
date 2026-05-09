"""
Streamable HTTP MCP — numeric tools (default MCP path ``/mcp``).

Gateway registration
--------------------
- **Adapter URL**: ``http://127.0.0.1:9101`` (or host.docker.internal from containers)
- **Type**: Streamable HTTP

Run::

    cd example && uv sync && uv run uvicorn servers.streamable_calculator:app --host 0.0.0.0 --port 9101
"""

from __future__ import annotations

import os

from mcp.server.fastmcp import FastMCP

from servers._asgi import build_streamable_app

PORT = int(os.environ.get("PORT", "9101"))

mcp = FastMCP("Example Calculator", json_response=True)


@mcp.tool()
def add(a: int, b: int) -> int:
    """Add two integers."""
    return a + b


@mcp.tool()
def multiply(a: float, b: float) -> float:
    """Multiply two numbers."""
    return a * b


@mcp.tool()
def factorial(n: int) -> int:
    """Factorial of a small non-negative integer (n <= 20)."""
    if n < 0 or n > 20:
        raise ValueError("n must be between 0 and 20")
    out = 1
    for i in range(2, n + 1):
        out *= i
    return out


app = build_streamable_app(mcp=mcp, service_id="streamable-calculator", port=PORT)
