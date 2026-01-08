FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Environment variables will be set at runtime
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BeC.OpenId.Connect/BeC.OpenId.Connect.csproj", "BeC.OpenId.Connect/"]
RUN dotnet restore "BeC.OpenId.Connect/BeC.OpenId.Connect.csproj"
COPY . .
WORKDIR "/src/BeC.OpenId.Connect"
RUN dotnet build "./BeC.OpenId.Connect.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BeC.OpenId.Connect.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BeC.OpenId.Connect.dll"]
