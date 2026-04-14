
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SmartQueue.Api.csproj", "."]
RUN dotnet restore "./SmartQueue.Api.csproj"
COPY . .
RUN dotnet build "./SmartQueue.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./SmartQueue.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartQueue.Api.dll"]