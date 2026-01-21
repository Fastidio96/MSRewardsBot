# =========================
# Stage 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build

ARG BUILD_CONFIGURATION=Release

# Install git and tools first
RUN apt-get update \
    && apt-get install -y --no-install-recommends git wget ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Verify git is available
RUN git --version

# Set working directory inside container
WORKDIR /src

# Copy csproj files first to leverage caching
COPY Applications/MSRewardsBot.Server/MSRewardsBot.Server.csproj Applications/MSRewardsBot.Server/
COPY Libraries/MSRewardsBot.Common/MSRewardsBot.Common.csproj Libraries/MSRewardsBot.Common/

# Restore dependencies
RUN dotnet restore Applications/MSRewardsBot.Server/MSRewardsBot.Server.csproj

# Copy source code (excluding client via .dockerignore)
COPY Applications/MSRewardsBot.Server/ Applications/MSRewardsBot.Server/
COPY Libraries/MSRewardsBot.Common/ Libraries/MSRewardsBot.Common/

# Build and publish
WORKDIR /src/Applications/MSRewardsBot.Server
RUN dotnet publish MSRewardsBot.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =========================
# Stage 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS runtime

WORKDIR /app

# Install minimal dependencies for Playwright browsers
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    fonts-liberation \
    wget \
    libnss3 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libxrender1 \
    libxcb1 \
    libxcb-shm0 \
    libx11-6 \
    libx11-xcb1 \
    libxext6 \
    libxcursor1 \
    libxi6 \
    libgbm1 \
    libasound2 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    libgtk-3-0 \
    libgdk-pixbuf-2.0-0 \
    libglib2.0-0 \
    libcairo2 \
    libcairo-gobject2 \
    libdbus-1-3 \
    libfreetype6 \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=build /app/publish .

# Create volume mount point for MSRB folder
VOLUME ["/app/MSRB"]

# Set entrypoint
ENTRYPOINT ["dotnet", "MSRewardsBot.Server.dll"]
