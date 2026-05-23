# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["JiraDataFrontend.csproj", "."]
RUN dotnet restore "JiraDataFrontend.csproj"

COPY [".", "."]
RUN dotnet build "JiraDataFrontend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JiraDataFrontend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "JiraDataFrontend.dll"]
