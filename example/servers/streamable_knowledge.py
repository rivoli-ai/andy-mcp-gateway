"""
Streamable HTTP MCP — text tools plus a resource and a prompt.

Gateway registration
--------------------
- **Adapter URL**: ``http://127.0.0.1:9102``
- **Type**: Streamable HTTP

Run::

    cd example && uv run uvicorn servers.streamable_knowledge:app --host 0.0.0.0 --port 9102
"""

from __future__ import annotations

import os
import re

from mcp.server.fastmcp import FastMCP

from servers._asgi import build_streamable_app

PORT = int(os.environ.get("PORT", "9102"))

mcp = FastMCP("Example Knowledge Base", json_response=True)


@mcp.tool()
def echo(message: str) -> str:
    """Return the same message (useful for connectivity checks)."""
    return message


@mcp.tool()
def word_count(text: str) -> int:
    """Count whitespace-separated words in ``text``."""
    return len(re.findall(r"\S+", text))


@mcp.resource("kb://overview")
def knowledge_overview() -> str:
    """Short overview of this example MCP for agents."""
    return (
        "This is the streamable_knowledge example server. "
        "It exposes echo/word_count tools and kb://overview resource."
    )


@mcp.prompt()
def summarize_topic(topic: str, tone: str = "neutral") -> str:
    """Ask the model to summarize a topic in a given tone."""
    return f"In a {tone} tone, summarize: {topic}"


app = build_streamable_app(mcp=mcp, service_id="streamable-knowledge", port=PORT)
