FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Reflect.ProviderDashboard.Domain/Reflect.ProviderDashboard.Domain.csproj Reflect.ProviderDashboard.Domain/
COPY Reflect.ProviderDashboard.Web/Reflect.ProviderDashboard.Web.csproj Reflect.ProviderDashboard.Web/
RUN dotnet restore Reflect.ProviderDashboard.Web/Reflect.ProviderDashboard.Web.csproj
COPY Reflect.ProviderDashboard.Domain/ Reflect.ProviderDashboard.Domain/
COPY Reflect.ProviderDashboard.Web/ Reflect.ProviderDashboard.Web/
RUN dotnet publish Reflect.ProviderDashboard.Web/Reflect.ProviderDashboard.Web.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
RUN mkdir -p /app/App_Data && chown app:app /app/App_Data && chmod 700 /app/App_Data
ENV ASPNETCORE_HTTP_PORTS=8080 ASPNETCORE_ENVIRONMENT=Production
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Reflect.ProviderDashboard.Web.dll"]
