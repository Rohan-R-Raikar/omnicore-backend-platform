FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/OmniCore.ProductService/OmniCore.ProductService.csproj", "Services/OmniCore.ProductService/"]
COPY ["Shared/OmniCore.Shared/OmniCore.Shared.csproj", "Shared/OmniCore.Shared/"]

RUN dotnet restore "Services/OmniCore.ProductService/OmniCore.ProductService.csproj"

COPY . .

WORKDIR "/src/Services/OmniCore.ProductService"

RUN dotnet build "OmniCore.ProductService.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

FROM build AS publish

RUN dotnet publish "OmniCore.ProductService.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OmniCore.ProductService.dll"]