FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY ["src/McpGateway/McpGateway.csproj", "src/McpGateway/"]
COPY ["src/McpGateway.Application/McpGateway.Application.csproj", "src/McpGateway.Application/"]
COPY ["src/McpGateway.Domain/McpGateway.Domain.csproj", "src/McpGateway.Domain/"]
COPY ["src/McpGateway.Infrastructure/McpGateway.Infrastructure.csproj", "src/McpGateway.Infrastructure/"]
RUN dotnet restore "src/McpGateway/McpGateway.csproj"
COPY src ./src
WORKDIR "/build/src/McpGateway"
RUN dotnet build "McpGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpGateway.dll"]
