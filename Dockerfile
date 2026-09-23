FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ["src/Identity.Domain/Identity.Domain.csproj", "src/Identity.Domain/"]
COPY ["src/Identity.Application/Identity.Application.csproj", "src/Identity.Application/"]
COPY ["src/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Identity.Infrastructure/"]
COPY ["src/Identity.API/Identity.API.csproj", "src/Identity.API/"]

RUN dotnet restore "src/Identity.API/Identity.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/app/src/Identity.API"
RUN dotnet build "Identity.API.csproj" -c Release -o /app/build
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
