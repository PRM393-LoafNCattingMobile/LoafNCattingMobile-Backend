FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY LoafNCatting.Api/LoafNCatting.Api.csproj LoafNCatting.Api/
COPY LoafNCatting.Data/LoafNCatting.Data.csproj LoafNCatting.Data/
COPY LoafNCatting.Service/LoafNCatting.Service.csproj LoafNCatting.Service/
RUN dotnet restore LoafNCatting.Api/LoafNCatting.Api.csproj

COPY . .
RUN dotnet publish LoafNCatting.Api/LoafNCatting.Api.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoafNCatting.Api.dll"]
