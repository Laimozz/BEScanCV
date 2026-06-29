# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BEScanCV.API/BEScanCV.API.csproj", "BEScanCV.API/"]
COPY ["BEScanCV.Application/BEScanCV.Application.csproj", "BEScanCV.Application/"]
COPY ["BEScanCV.Domain/BEScanCV.Domain.csproj", "BEScanCV.Domain/"]
COPY ["BEScanCV.Infrastructure/BEScanCV.Infrastructure.csproj", "BEScanCV.Infrastructure/"]

RUN dotnet restore "BEScanCV.API/BEScanCV.API.csproj"

COPY . .
RUN dotnet publish "BEScanCV.API/BEScanCV.API.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BEScanCV.API.dll"]
