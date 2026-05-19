# Use .NET 6.0 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy csproj files and restore dependencies
COPY *.sln ./
COPY PediMix.API/*.csproj ./PediMix.API/
COPY PediMix.Application/*.csproj ./PediMix.Application/
COPY PediMix.Domain/*.csproj ./PediMix.Domain/
COPY PediMix.Infrastructure/*.csproj ./PediMix.Infrastructure/

RUN dotnet restore

# Copy source code and build
COPY . ./
RUN dotnet publish PediMix.API/PediMix.API.csproj -c Release -o out

# Use .NET 6.0 runtime for production
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Copy the built application
COPY --from=build /app/out ./

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "PediMix.API.dll"]
