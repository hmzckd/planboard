FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Planboard.Api/Planboard.Api.csproj src/Planboard.Api/
RUN dotnet restore src/Planboard.Api/Planboard.Api.csproj
COPY src/Planboard.Api/ src/Planboard.Api/
RUN dotnet publish src/Planboard.Api/Planboard.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Planboard.Api.dll"]
