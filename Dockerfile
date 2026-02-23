# =========================
# Stage 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build

ARG BUILD_CONFIGURATION=Release

# Install git (needed for dotnet restore) and minimal tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    git \
    wget \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

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

# Install Node.js for Playwright only for build stage
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y nodejs

# Install Playwright browsers
RUN npm install playwright@1.58.0

# Build and publish
WORKDIR /src/Applications/MSRewardsBot.Server
RUN dotnet publish MSRewardsBot.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# =========================
# Stage 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS runtime

# Environment variables
ENV IsHttpsEnabled=false
ENV ServerHost=0.0.0.0
ENV ServerPort=10500
ENV IsClientUpdaterEnabled=false
ENV UseFirefox=true
ENV MinSecsWaitBetweenSearches=180
ENV MaxSecsWaitBetweenSearches=300
ENV DashboardCheck="12:00:00"
ENV SearchesCheck="06:00:00"
ENV KeywordsListRefresh="03:00:00"
ENV KeywordsListCountries="IT,US,GB,DE,FR,ES"
ENV WriteLogsOnFile=true
ENV LogsGroupedCategories=true
ENV MinimumLogLevel="Debug"

# Reset .NET environment variables
ENV ASPNETCORE_HTTP_PORTS=
ENV ASPNETCORE_HTTPS_PORTS=

WORKDIR /app

# Install Playwright dependencies for Ubuntu 24.04
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    fonts-liberation \
    wget \
    git \
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
    libasound2t64 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    libgtk-4-1 \
    gstreamer1.0-plugins-base \
    gstreamer1.0-plugins-good \
    gstreamer1.0-plugins-bad \
    gstreamer1.0-libav \
    libsecret-1-0 \
    libhyphen0 \
    libnghttp2-14 \
    woff2 \
    libatomic1 \                      
    libevent-2.1-7t64 \                           
    libwebpdemux2 \                               
    libavif16 \                                   
    libharfbuzz-icu0 \                            
    libenchant-2-2 \                              
    libmanette-0.2-0 \     
    && rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=build /app/publish .

# Create volume mount point for MSRB folder
VOLUME ["/app/MSRB"]

# Expose port
EXPOSE 10500

# Set entrypoint
ENTRYPOINT ["dotnet", "MSRewardsBot.Server.dll"]
