# Runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CryptoAggregatorPro.csproj", "."]
RUN dotnet restore "CryptoAggregatorPro.csproj"
COPY . .
RUN dotnet build "CryptoAggregatorPro.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "CryptoAggregatorPro.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CryptoAggregatorPro.dll"]
