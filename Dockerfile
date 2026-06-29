# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY WorldCupPredictor.sln ./
COPY src/WorldCupPredictor.API/WorldCupPredictor.API.csproj ./src/WorldCupPredictor.API/
RUN dotnet restore

COPY src/WorldCupPredictor.API/ ./src/WorldCupPredictor.API/
RUN dotnet publish src/WorldCupPredictor.API/WorldCupPredictor.API.csproj \
    -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render free tier expects port 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "WorldCupPredictor.API.dll"]
