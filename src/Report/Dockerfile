FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Report/MediaTrust.Report.csproj src/Report/
RUN dotnet restore src/Report/MediaTrust.Report.csproj

COPY src/Report/ src/Report/
RUN dotnet publish src/Report/MediaTrust.Report.csproj -c Release --no-restore -o /publish/report /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /publish/report/ .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MediaTrust.Report.dll"]
