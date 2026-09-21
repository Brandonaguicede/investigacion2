using WEBSOCKET.WebSockets.Connections;
using WEBSOCKET.WebSockets;
using Auction.Application.Abstractions.Persistence;
using Auction.Application.Auctions.GetAuctionState;
using Auction.Application.Bids.PlaceBid;
using Auction.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
builder.Services.AddSingleton<IAuctionStateStore, InMemoryAuctionStateStore>();
builder.Services.AddSingleton<PlaceBidService>();
builder.Services.AddSingleton<GetAuctionStateService>();
builder.Services.AddSingleton<WebSocketMessageSender>();
builder.Services.AddSingleton<AuctionWebSocketHandler>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled", true))
{
    app.UseHttpsRedirection();
}

app.UseWebSockets();

app.Map("/ws", async (HttpContext context, AuctionWebSocketHandler handler) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    await handler.HandleAsync(context);
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
