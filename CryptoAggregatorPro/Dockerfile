# Runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the csproj and restore dependencies
COPY ["CryptoAggregatorPro.csproj", "."]
RUN dotnet restore "CryptoAggregatorPro.csproj"

# Copy all project files
COPY . .

# Build the app
RUN dotnet build "CryptoAggregatorPro.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "CryptoAggregatorPro.csproj" -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app

# Copy published files from the build stage
COPY --from=publish /app/publish .

# Set entry point for the application
ENTRYPOINT ["dotnet", "CryptoAggregatorPro.dll"]
