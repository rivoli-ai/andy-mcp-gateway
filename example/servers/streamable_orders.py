"""
Streamable HTTP MCP — structured tool output (Pydantic-style typing).

Gateway registration
--------------------
- **Adapter URL**: ``http://127.0.0.1:9104``
- **Type**: Streamable HTTP

Run::

    cd example && uv run uvicorn servers.streamable_orders:app --host 0.0.0.0 --port 9104
"""

from __future__ import annotations

import os
import uuid
from datetime import UTC, datetime

from mcp.server.fastmcp import FastMCP
from pydantic import BaseModel, Field

from servers._asgi import build_streamable_app

PORT = int(os.environ.get("PORT", "9104"))

mcp = FastMCP("Example Orders", json_response=True)


class OrderLine(BaseModel):
    sku: str = Field(description="Stock keeping unit")
    quantity: int = Field(ge=1, description="Quantity ordered")


class OrderResult(BaseModel):
    order_id: str
    created_at: str
    lines: list[OrderLine]
    total_units: int


@mcp.tool()
def place_order(lines: list[OrderLine]) -> OrderResult:
    """Create a fake order from line items and return structured confirmation."""
    oid = str(uuid.uuid4())
    total = sum(line.quantity for line in lines)
    return OrderResult(
        order_id=oid,
        created_at=datetime.now(UTC).isoformat(),
        lines=lines,
        total_units=total,
    )


@mcp.tool()
def lookup_sku(sku: str) -> dict[str, str | int]:
    """Return placeholder catalog metadata for a SKU."""
    return {"sku": sku.upper(), "stock": 42, "warehouse": "DEMO-1"}


app = build_streamable_app(mcp=mcp, service_id="streamable-orders", port=PORT)
