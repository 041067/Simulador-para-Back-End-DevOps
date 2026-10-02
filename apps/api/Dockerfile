FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BackOpsSimulator.sln", "."]
COPY ["src/BackOps.Domain/BackOps.Domain.csproj", "src/BackOps.Domain/"]
COPY ["src/BackOps.Application/BackOps.Application.csproj", "src/BackOps.Application/"]
COPY ["src/BackOps.Infrastructure/BackOps.Infrastructure.csproj", "src/BackOps.Infrastructure/"]
COPY ["src/BackOps.Contracts/BackOps.Contracts.csproj", "src/BackOps.Contracts/"]
COPY ["apps/api/BackOps.Api.csproj", "apps/api/"]
COPY ["apps/worker/BackOps.Worker.csproj", "apps/worker/"]
RUN dotnet restore "BackOpsSimulator.sln"

COPY . .
WORKDIR "/src/apps/api"
RUN dotnet build "BackOps.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BackOps.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BackOps.Api.dll"]