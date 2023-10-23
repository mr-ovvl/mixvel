FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5100

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY src /src
WORKDIR /src
RUN dotnet restore "MixVel.Api/MixVel.Api.csproj"
WORKDIR "/src/MixVel.Api"
RUN dotnet build "MixVel.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MixVel.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MixVel.Api.dll"]