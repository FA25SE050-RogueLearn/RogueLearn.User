# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 6968
EXPOSE 6969


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/RogueLearn.User.Api/RogueLearn.User.Api.csproj", "src/RogueLearn.User.Api/"]
COPY ["src/RogueLearn.User.Infrastructure/RogueLearn.User.Infrastructure.csproj", "src/RogueLearn.User.Infrastructure/"]
COPY ["building_blocks/BuildingBlocks.Shared/BuildingBlocks.Shared.csproj", "building_blocks/BuildingBlocks.Shared/"]
COPY ["src/RogueLearn.User.Application/RogueLearn.User.Application.csproj", "src/RogueLearn.User.Application/"]
COPY ["src/RogueLearn.User.Domain/RogueLearn.User.Domain.csproj", "src/RogueLearn.User.Domain/"]
RUN dotnet restore "./src/RogueLearn.User.Api/RogueLearn.User.Api.csproj"
COPY . .
WORKDIR "/src/src/RogueLearn.User.Api"
RUN dotnet build "./RogueLearn.User.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RogueLearn.User.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RogueLearn.User.Api.dll"]