FROM node:22-bookworm-slim AS frontend-build

WORKDIR /src/Client
COPY src/Client/package.json src/Client/package-lock.json ./
RUN npm ci
COPY src/Client/ ./

# Tie the Angular build layer to the release so stale remote BuildKit layers are not reused.
ARG ANGULAR_BUILD_VERSION=local
RUN printf 'Building Angular bundle %s\n' "$ANGULAR_BUILD_VERSION" \
    && npm run build -- --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "src/ServiceDefaults/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Web/Web.csproj"

COPY . .
RUN dotnet publish "src/Web/Web.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false
COPY --from=frontend-build /src/Client/dist/ske/browser /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "skestock.Web.dll"]
