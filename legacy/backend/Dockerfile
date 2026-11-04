FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia la solución, los proyectos del backend y la carpeta de tests
COPY ["backend/AlertaClimatica.slnx", "backend/"]
COPY ["backend/AlertaClimatica.Api/AlertaClimatica.Api.csproj", "backend/AlertaClimatica.Api/"]
COPY ["backend/AlertaClimatica.Application/AlertaClimatica.Application.csproj", "backend/AlertaClimatica.Application/"]
COPY ["backend/AlertaClimatica.Domain/AlertaClimatica.Domain.csproj", "backend/AlertaClimatica.Domain/"]
COPY ["backend/AlertaClimatica.Infrastructure/AlertaClimatica.Infrastructure.csproj", "backend/AlertaClimatica.Infrastructure/"]

# Copia los proyectos de pruebas ya que la solución los referencia
COPY ["tests/", "tests/"]

RUN dotnet restore "backend/AlertaClimatica.slnx"

# Copia el resto del código fuente del backend y tests
COPY backend/ backend/
WORKDIR /src/backend/AlertaClimatica.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5081
ENV ASPNETCORE_URLS=http://+:5081
ENTRYPOINT ["dotnet", "AlertaClimatica.Api.dll"]