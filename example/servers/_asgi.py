"""Shared ASGI helpers: gateway health checks hit ``GET /health`` on the upstream base URL."""

from __future__ import annotations

import contextlib
from collections.abc import AsyncIterator, Callable, Sequence

from mcp.server.fastmcp import FastMCP
from starlette.applications import Starlette
from starlette.middleware import Middleware
from starlette.middleware.cors import CORSMiddleware
from starlette.responses import JSONResponse
from starlette.routing import Mount, Route


def health_handler(service_id: str, port: int) -> Callable[..., JSONResponse]:
    async def health(_):  # type: ignore[no-untyped-def]
        return JSONResponse(
            {
                "status": "healthy",
                "service": service_id,
                "port": port,
            }
        )

    return health


def mcp_cors() -> list[Middleware]:
    return [
        Middleware(
            CORSMiddleware,
            allow_origins=["*"],
            allow_methods=["GET", "POST", "DELETE", "OPTIONS"],
            allow_headers=["*"],
            expose_headers=["Mcp-Session-Id"],
        ),
    ]


@contextlib.asynccontextmanager
async def mcp_lifespan(mcp: FastMCP) -> AsyncIterator[None]:
    async with mcp.session_manager.run():
        yield


def build_streamable_app(
    *,
    mcp: FastMCP,
    service_id: str,
    port: int,
    enable_cors: bool = True,
) -> Starlette:
    @contextlib.asynccontextmanager
    async def lifespan(app: Starlette) -> AsyncIterator[None]:
        async with mcp_lifespan(mcp):
            yield

    routes: Sequence[Route | Mount] = [
        Route("/health", health_handler(service_id, port), methods=["GET"]),
        Mount("/", app=mcp.streamable_http_app()),
    ]
    middleware = mcp_cors() if enable_cors else []
    return Starlette(routes=list(routes), lifespan=lifespan, middleware=middleware)


def build_sse_app(
    *,
    mcp: FastMCP,
    service_id: str,
    port: int,
    enable_cors: bool = True,
) -> Starlette:
    # SSE transport does not use the streamable HTTP session manager; do not wrap
    # ``mcp.session_manager.run()`` here (it is only created after ``streamable_http_app()``).
    routes: Sequence[Route | Mount] = [
        Route("/health", health_handler(service_id, port), methods=["GET"]),
        Mount("/", app=mcp.sse_app()),
    ]
    middleware = mcp_cors() if enable_cors else []
    return Starlette(routes=list(routes), middleware=middleware)
