# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file changes.
COPY Directory.Build.props SpottifyDwnldr.slnx ./
COPY src/Spottify.Core/Spottify.Core.csproj src/Spottify.Core/
COPY src/Spottify.Infrastructure/Spottify.Infrastructure.csproj src/Spottify.Infrastructure/
COPY src/Spottify.Host/Spottify.Host.csproj src/Spottify.Host/
RUN dotnet restore src/Spottify.Host/Spottify.Host.csproj

COPY src/ src/
RUN dotnet publish src/Spottify.Host/Spottify.Host.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# State lives in /data; mount a volume there. The base image's non-root user keeps the container unprivileged.
ENV SPOTTIFY_Spottify__DataDirectory=/data \
    ASPNETCORE_URLS=http://+:8080
RUN mkdir /data && chown $APP_UID /data
VOLUME /data
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "Spottify.Host.dll"]
