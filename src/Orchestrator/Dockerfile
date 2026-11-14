FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Orchestrator/MediaTrust.Orchestrator.csproj src/Orchestrator/
RUN dotnet restore src/Orchestrator/MediaTrust.Orchestrator.csproj

COPY src/Orchestrator/ src/Orchestrator/
RUN dotnet publish src/Orchestrator/MediaTrust.Orchestrator.csproj -c Release --no-restore -o /publish/orchestrator /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /publish/orchestrator/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaTrust.Orchestrator.dll"]
