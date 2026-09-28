FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["LOOP.API/LOOP.API.csproj", "LOOP.API/"]

RUN dotnet restore "LOOP.API/LOOP.API.csproj"

COPY . .

WORKDIR "/src/LOOP.API"

RUN dotnet publish "LOOP.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LOOP.API.dll"]