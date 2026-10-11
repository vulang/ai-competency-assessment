ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
FROM ${SDK_IMAGE} AS build
WORKDIR /source
COPY backend/AiCompetency.Api/AiCompetency.Api.csproj backend/AiCompetency.Api/
RUN dotnet restore backend/AiCompetency.Api/AiCompetency.Api.csproj
COPY backend/AiCompetency.Api backend/AiCompetency.Api
RUN dotnet publish backend/AiCompetency.Api/AiCompetency.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 HOME=/home/app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /home/app/.aspnet /home/app/.dotnet && chown -R app:app /home/app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
HEALTHCHECK --interval=5s --timeout=3s --start-period=20s CMD curl --fail --silent http://127.0.0.1:8080/health || exit 1
ENTRYPOINT ["dotnet", "AiCompetency.Api.dll"]
