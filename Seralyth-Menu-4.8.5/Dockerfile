# Seralyth Menu API - container image for Linux hosts (Fly.io, Azure, Railway, Docker)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Server/Server.csproj Server/
RUN dotnet restore Server/Server.csproj
COPY Server/ Server/
RUN dotnet publish Server/Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Cloud hosts inject PORT; the app already binds 0.0.0.0:$PORT (see Program.cs).
ENV PORT=8080
ENV DATA_DIR=/data
EXPOSE 8080

# Friend lists, votes and ban reports live here - mount a volume so they survive restarts.
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "Seralyth.Menu.Server.dll"]
