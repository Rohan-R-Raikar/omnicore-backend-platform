FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/OmniCore.AuthService/OmniCore.AuthService.csproj", "Services/OmniCore.AuthService/"]
COPY ["Shared/OmniCore.Shared/OmniCore.Shared.csproj", "Shared/OmniCore.Shared/"]

RUN dotnet restore "Services/OmniCore.AuthService/OmniCore.AuthService.csproj"

COPY . .

WORKDIR "/src/Services/OmniCore.AuthService"

RUN dotnet build "OmniCore.AuthService.csproj" \
    -c Release \
    -o /app/build \
    --no-restore

FROM build AS publish

RUN dotnet publish "OmniCore.AuthService.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "OmniCore.AuthService.dll"]