# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY ["E-Commerce.csproj", "./"]
RUN dotnet restore "E-Commerce.csproj"

# Copy source code
COPY . .

# Build
RUN dotnet build "E-Commerce.csproj" -c Release -o /app/build

# Publish stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /app
COPY --from=build /app/build .
RUN dotnet publish "E-Commerce.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 10000

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

# Run
ENTRYPOINT ["dotnet", "E-Commerce.dll"]
