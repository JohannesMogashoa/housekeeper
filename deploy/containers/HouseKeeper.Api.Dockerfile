# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/HouseKeeper.Api/HouseKeeper.Api.csproj src/HouseKeeper.Api/
COPY src/HouseKeeper.Contracts/HouseKeeper.Contracts.csproj src/HouseKeeper.Contracts/
COPY src/HouseKeeper.BuildingBlocks/HouseKeeper.BuildingBlocks.csproj src/HouseKeeper.BuildingBlocks/
COPY src/Modules/HouseKeeper.Modules.Attachments/HouseKeeper.Modules.Attachments.csproj src/Modules/HouseKeeper.Modules.Attachments/
COPY src/Modules/HouseKeeper.Modules.Households/HouseKeeper.Modules.Households.csproj src/Modules/HouseKeeper.Modules.Households/
COPY src/Modules/HouseKeeper.Modules.Maintenance/HouseKeeper.Modules.Maintenance.csproj src/Modules/HouseKeeper.Modules.Maintenance/
COPY src/Modules/HouseKeeper.Modules.Notifications/HouseKeeper.Modules.Notifications.csproj src/Modules/HouseKeeper.Modules.Notifications/
COPY src/Modules/HouseKeeper.Modules.Shopping/HouseKeeper.Modules.Shopping.csproj src/Modules/HouseKeeper.Modules.Shopping/
COPY src/Modules/HouseKeeper.Modules.Tasks/HouseKeeper.Modules.Tasks.csproj src/Modules/HouseKeeper.Modules.Tasks/

RUN dotnet restore src/HouseKeeper.Api/HouseKeeper.Api.csproj

COPY src src
RUN dotnet publish src/HouseKeeper.Api/HouseKeeper.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /out .

# The ASP.NET runtime image provides the unprivileged `app` user.
USER app
ENTRYPOINT ["dotnet", "HouseKeeper.Api.dll"]
