FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Viewer.sln", "Viewer/"]
COPY ["Viewer.Tests/Viewer.Tests.csproj", "Viewer/Viewer.Tests/"]
COPY ["Server/Viewer.Server.csproj", "Viewer/Server/"]
COPY ["Client/Viewer.Client.csproj", "Viewer/Client/"]
COPY ["Shared/Viewer.Shared.csproj", "Viewer/Shared/"]
RUN dotnet restore "Viewer/Viewer.sln"
WORKDIR "/src/Viewer"
COPY . .
RUN dotnet build "Server/Viewer.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Server/Viewer.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Viewer.Server.dll"]
