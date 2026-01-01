FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Gateway/MediaTrust.Gateway.csproj src/Gateway/
RUN dotnet restore src/Gateway/MediaTrust.Gateway.csproj
COPY . .
WORKDIR /src/src/Gateway
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MediaTrust.Gateway.dll"]