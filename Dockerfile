FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CalidadYPrueba_T3/CalidadYPrueba_T3.csproj CalidadYPrueba_T3/
RUN dotnet restore CalidadYPrueba_T3/CalidadYPrueba_T3.csproj
COPY CalidadYPrueba_T3/ CalidadYPrueba_T3/
RUN dotnet publish CalidadYPrueba_T3/CalidadYPrueba_T3.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID:$APP_UID /home/app/.aspnet
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CalidadYPrueba_T3.dll"]
