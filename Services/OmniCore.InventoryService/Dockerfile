FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/OmniCore.InventoryService/OmniCore.InventoryService.csproj", "Services/OmniCore.InventoryService/"]
COPY ["Shared/OmniCore.Shared/OmniCore.Shared.csproj", "Shared/OmniCore.Shared/"]

RUN dotnet restore "Services/OmniCore.InventoryService/OmniCore.InventoryService.csproj"

COPY . .

WORKDIR "/src/Services/OmniCore.InventoryService"

RUN dotnet build "OmniCore.InventoryService.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

FROM build AS publish

RUN dotnet publish "OmniCore.InventoryService.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OmniCore.InventoryService.dll"]