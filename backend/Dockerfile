# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RentasApi.csproj", "./"]
RUN dotnet restore "RentasApi.csproj"
COPY . .
RUN dotnet publish "RentasApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Serve Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Crear directorio para la DB y adjuntos
RUN mkdir -p /data/uploads
ENV DB_PATH=/data/rentas.db
ENV Uploads__Path=/data/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "RentasApi.dll"]
