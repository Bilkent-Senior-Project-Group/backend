# Step 1: Use the .NET 8 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory to /app
WORKDIR /app

# Copy the project file and restore dependencies
COPY CompanyHubService/CompanyHubService/CompanyHubService.csproj CompanyHubService/CompanyHubService/
RUN dotnet restore CompanyHubService/CompanyHubService/CompanyHubService.csproj

# Copy the rest of the files from the project and publish the app
COPY . .
RUN dotnet publish CompanyHubService/CompanyHubService/CompanyHubService.csproj -c Release -o /app/publish

# Step 2: Create the runtime image using the .NET 8 runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Set the working directory to /app for runtime
WORKDIR /app

# Copy the published app from the build stage
COPY --from=build /app/publish .

# Step 3: Define the entry point
ENTRYPOINT ["dotnet", "CompanyHubService.dll"]
