FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OnlySplit.API.sln", "./"]
COPY ["src/OnlySplit.API/OnlySplit.API.csproj", "src/OnlySplit.API/"]
COPY ["src/OnlySplit.Application/OnlySplit.Application.csproj", "src/OnlySplit.Application/"]
COPY ["src/OnlySplit.Domain/OnlySplit.Domain.csproj", "src/OnlySplit.Domain/"]
COPY ["src/OnlySplit.Infrastructure/OnlySplit.Infrastructure.csproj", "src/OnlySplit.Infrastructure/"]
COPY ["src/OnlySplit.Shared/OnlySplit.Shared.csproj", "src/OnlySplit.Shared/"]
RUN dotnet restore "OnlySplit.API.sln"

COPY . .
RUN dotnet publish "src/OnlySplit.API/OnlySplit.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OnlySplit.API.dll"]
