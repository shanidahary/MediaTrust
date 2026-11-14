FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Gateway/MediaTrust.Gateway.csproj src/Gateway/
RUN dotnet restore src/Gateway/MediaTrust.Gateway.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/MediaTrust.Gateway.csproj -c Release --no-restore -o /publish/gateway /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /publish/gateway/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaTrust.Gateway.dll"]
