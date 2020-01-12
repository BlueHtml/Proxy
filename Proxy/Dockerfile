#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["Proxy/Proxy.csproj", "Proxy/"]
RUN dotnet restore "Proxy/Proxy.csproj"
COPY . .
WORKDIR "/src/Proxy"
RUN dotnet build "Proxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Proxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "Proxy.dll"]
# heroku uses the following
CMD ASPNETCORE_URLS=http://*:$PORT dotnet Proxy.dll