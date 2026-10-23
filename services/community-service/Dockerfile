FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY services/shared-dotnet/Clima.Shared/*.csproj services/shared-dotnet/Clima.Shared/
COPY services/community-service/*.csproj services/community-service/
RUN dotnet restore services/community-service/Community.Service.csproj
COPY services/shared-dotnet services/shared-dotnet
COPY services/community-service services/community-service
RUN dotnet publish services/community-service/Community.Service.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 DOTNET_gcServer=0
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet","Community.Service.dll"]
