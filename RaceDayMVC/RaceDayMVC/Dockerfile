# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RaceDayMVC/RaceDayMVC.csproj", "RaceDayMVC/"]
RUN dotnet restore "./RaceDayMVC/RaceDayMVC.csproj"
COPY . .
WORKDIR "/src/RaceDayMVC"
RUN dotnet build "./RaceDayMVC.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RaceDayMVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Inside a container "localhost" is the container itself, so point at the API running on the Docker host.
# Override at run time with: -e ApiSettings__BaseUrl=http://your-api-address
ENV ApiSettings__BaseUrl=http://host.docker.internal:5068
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RaceDayMVC.dll"]
