# Use the official .NET runtime as base image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["SF-Server/SF-Server.csproj", "SF-Server/"]

# Restore dependencies
RUN dotnet restore "SF-Server/SF-Server.csproj"

# Copy source code
COPY SF-Server/ SF-Server/

# Build the application
WORKDIR "/src/SF-Server"
RUN dotnet build "SF-Server.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SF-Server.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app

# Create a non-root user
RUN groupadd -r sfserver && useradd -r -g sfserver sfserver

# Copy the published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R sfserver:sfserver /app

# Switch to non-root user
USER sfserver

# Expose the default port
EXPOSE 1337/udp

# Set environment variables with defaults
ENV SF_PORT=1337
ENV SF_MAX_PLAYERS=4
ENV SF_ENABLE_LOGGING=true

# Create entrypoint script
COPY --chown=sfserver:sfserver <<EOF /app/entrypoint.sh
#!/bin/bash
set -e

# Check required environment variables
if [ -z "\$SF_STEAM_WEB_API_TOKEN" ]; then
    echo "Error: SF_STEAM_WEB_API_TOKEN environment variable is required"
    exit 1
fi

if [ -z "\$SF_HOST_STEAMID" ]; then
    echo "Error: SF_HOST_STEAMID environment variable is required"
    exit 1
fi

# Build command line arguments
ARGS="--port \$SF_PORT --max_players \$SF_MAX_PLAYERS"
ARGS="\$ARGS --steam_web_api_token \$SF_STEAM_WEB_API_TOKEN"
ARGS="\$ARGS --host_steamid \$SF_HOST_STEAMID"

# Add optional configuration file
if [ ! -z "\$SF_CONFIG_FILE" ] && [ -f "\$SF_CONFIG_FILE" ]; then
    ARGS="\$SF_CONFIG_FILE \$ARGS"
fi

echo "Starting SF-Server with args: \$ARGS"
exec dotnet SF-Server.dll \$ARGS
EOF

RUN chmod +x /app/entrypoint.sh

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD netstat -an | grep 1337 > /dev/null; if [ 0 != $? ]; then exit 1; fi;

ENTRYPOINT ["/app/entrypoint.sh"]