#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS base
WORKDIR /app
RUN apk update && apk add ffmpeg

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY ["VideoEncoder/VideoEncoder.fsproj", "VideoEncoder/"]
RUN dotnet restore "VideoEncoder/VideoEncoder.fsproj"
COPY . .
WORKDIR "/src/VideoEncoder"
RUN dotnet build "VideoEncoder.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VideoEncoder.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoEncoder.dll"]