FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Ingest/MediaTrust.Ingest.csproj src/Ingest/
RUN dotnet restore src/Ingest/MediaTrust.Ingest.csproj

COPY src/Ingest/ src/Ingest/
RUN dotnet publish src/Ingest/MediaTrust.Ingest.csproj -c Release --no-restore -o /publish/ingest /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /publish/ingest/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaTrust.Ingest.dll"]
