FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OrgManagement.WebApi/OrgManagement.WebApi.csproj", "src/OrgManagement.WebApi/"]
COPY ["src/OrgManagement.Application/OrgManagement.Application.csproj", "src/OrgManagement.Application/"]
COPY ["src/OrgManagement.Domain/OrgManagement.Domain.csproj", "src/OrgManagement.Domain/"]
COPY ["src/OrgManagement.Infrastructure/OrgManagement.Infrastructure.csproj", "src/OrgManagement.Infrastructure/"]
RUN dotnet restore "src/OrgManagement.WebApi/OrgManagement.WebApi.csproj"
COPY . .
WORKDIR "/src/src/OrgManagement.WebApi"
RUN dotnet build "OrgManagement.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrgManagement.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrgManagement.WebApi.dll"]
