#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 7798
ENV ASPNETCORE_URLS=http://+:7798

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["chatgot/chatgot.csproj", "chatgot/"]
RUN dotnet restore "chatgot/chatgot.csproj"
COPY . .
WORKDIR "/src/chatgot"
RUN dotnet build "chatgot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "chatgot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "chatgot.dll"]
