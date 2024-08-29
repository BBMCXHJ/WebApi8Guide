#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebApi8Guide/WebApi8Guide.csproj", "WebApi8Guide/"]
COPY ["Web.Data/Web.Data.csproj", "Web.Data/"]
RUN dotnet restore "./WebApi8Guide/WebApi8Guide.csproj"
COPY . .
WORKDIR "/src/WebApi8Guide"
RUN dotnet build "./WebApi8Guide.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebApi8Guide.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi8Guide.dll"]