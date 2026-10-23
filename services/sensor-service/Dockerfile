FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/shared-dotnet/Clima.Shared/*.csproj services/shared-dotnet/Clima.Shared/
COPY services/sensor-service/*.csproj services/sensor-service/
RUN dotnet restore services/sensor-service/Sensor.Service.csproj
COPY services/shared-dotnet services/shared-dotnet
COPY services/sensor-service services/sensor-service
RUN dotnet publish services/sensor-service/Sensor.Service.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet","Sensor.Service.dll"]
