#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app
RUN apt-get update
RUN apt-get --yes install curl
ENV TZ="Europe/Copenhagen"
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DMIProxy/DMIProxy.csproj", "DMIProxy/"]
RUN dotnet restore "DMIProxy/DMIProxy.csproj"
COPY . .
WORKDIR "/src/DMIProxy"
RUN dotnet build "DMIProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DMIProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=60s --timeout=5s --retries=3 CMD curl --fail http://localhost:8080/healthcheck || exit 1

ENTRYPOINT ["dotnet", "DMIProxy.dll"]