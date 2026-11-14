FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Detectors/MediaTrust.Detectors.csproj src/Detectors/
RUN dotnet restore src/Detectors/MediaTrust.Detectors.csproj

COPY src/Detectors/ src/Detectors/
RUN dotnet publish src/Detectors/MediaTrust.Detectors.csproj -c Release --no-restore -o /publish/detectors /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /publish/detectors/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaTrust.Detectors.dll"]
