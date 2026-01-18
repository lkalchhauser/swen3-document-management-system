FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["DocumentManagementSystem.BatchWorker/DocumentManagementSystem.BatchWorker.csproj", "DocumentManagementSystem.BatchWorker/"]
COPY ["DocumentManagementSystem.DAL/DocumentManagementSystem.DAL.csproj", "DocumentManagementSystem.DAL/"]
COPY ["DocumentManagementSystem.Model/DocumentManagementSystem.Model.csproj", "DocumentManagementSystem.Model/"]

RUN dotnet restore "DocumentManagementSystem.BatchWorker/DocumentManagementSystem.BatchWorker.csproj"

COPY . .

WORKDIR "/src/DocumentManagementSystem.BatchWorker"
RUN dotnet build "DocumentManagementSystem.BatchWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentManagementSystem.BatchWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /data/batch/input /data/batch/archive /data/batch/error /logs

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "DocumentManagementSystem.BatchWorker.dll"]
