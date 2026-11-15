# Stage 1: The build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
copy . .
RUN dotnet publish API/API.csproj -c Release -o /app

# Stage 2: The ultimate running bruh
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "API.dll"]