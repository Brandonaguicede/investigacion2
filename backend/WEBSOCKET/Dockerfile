FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["backend/WEBSOCKET/WEBSOCKET.csproj", "backend/WEBSOCKET/"]
COPY ["backend/Auction.Domain/Auction.Domain.csproj", "backend/Auction.Domain/"]
COPY ["backend/Auction.Application/Auction.Application.csproj", "backend/Auction.Application/"]
COPY ["backend/Auction.Infrastructure/Auction.Infrastructure.csproj", "backend/Auction.Infrastructure/"]
RUN dotnet restore "backend/WEBSOCKET/WEBSOCKET.csproj"

COPY backend/Auction.Domain/ backend/Auction.Domain/
COPY backend/Auction.Application/ backend/Auction.Application/
COPY backend/Auction.Infrastructure/ backend/Auction.Infrastructure/
COPY backend/WEBSOCKET/ backend/WEBSOCKET/
RUN dotnet publish "backend/WEBSOCKET/WEBSOCKET.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    HttpsRedirection__Enabled=false
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WEBSOCKET.dll"]
