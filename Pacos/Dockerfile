#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pacos/Pacos.csproj", "Pacos/"]
RUN dotnet restore "Pacos/Pacos.csproj"
COPY . .
WORKDIR "/src/Pacos"
RUN dotnet build "Pacos.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pacos.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV DOTNET_ENVIRONMENT="Production"
ENV Pacos__TelegramBotApiKey=""
ENV Pacos__KoboldApiAddress=""
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pacos.dll"]
