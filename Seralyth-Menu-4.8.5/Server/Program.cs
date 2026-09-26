using System.Net.WebSockets;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Seralyth.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["PORT"]))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["PORT"]}");
}

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<ServerState>();
builder.Services.AddSingleton<FriendState>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<PollRefreshService>();

int voteLimit = int.TryParse(builder.Configuration["VOTE_LIMIT_PER_MINUTE"], out var configured) && configured > 0
    ? configured
    : 5;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("votes", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientKey.Resolve(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = voteLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

var friends = app.Services.GetRequiredService<FriendState>();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/")
    {
        string? userId = friends.ResolveUser(ClientKey.Resolve(context));
        if (userId is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        string mod = context.Request.Query["mod"].ToString();
        app.Logger.LogInformation("Friend socket connecting for {UserId} (mod {Mod})", userId, mod);

        using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        var relay = new FriendSocket(socket, friends, context.RequestServices.GetRequiredService<ILogger<FriendSocket>>());
        await relay.RunAsync(userId, context.RequestAborted);
        return;
    }

    await next();
});

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers.AccessControlAllowOrigin = "*";
    context.Response.Headers.AccessControlAllowMethods = "GET, POST, OPTIONS";
    context.Response.Headers.AccessControlAllowHeaders = "Content-Type";

    if (HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

var state = app.Services.GetRequiredService<ServerState>();

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    poll = state.PollKey,
    optionA = state.OptionA,
    optionB = state.OptionB,
    votes = state.GetCounts(),
    users = state.CountActiveUsers(),
    dataSource = state.DataUrl
}));

app.MapPost("/vote", (VoteRequest? request) =>
{
    string option = request?.Option ?? string.Empty;

    bool voteA = string.Equals(option, "a-votes", StringComparison.Ordinal);
    bool voteB = string.Equals(option, "b-votes", StringComparison.Ordinal);

    if (!voteA && !voteB)
        return Results.BadRequest(new { error = "option must be \"a-votes\" or \"b-votes\"" });

    VoteCounts counts = state.RecordVote(voteA);

    return Results.Ok(new
    {
        votes = counts,
        poll = state.PollKey,
        optionA = state.OptionA,
        optionB = state.OptionB
    });
}).RequireRateLimiting("votes");

app.MapGet("/usercount", () => Results.Ok(new
{
    mods = new
    {
        seralyth = new { users = state.CountActiveUsers() }
    }
}));

app.MapGet("/reportdata", () =>
{
    string? stored = state.ReadReportJson();
    return Results.Content(stored ?? "{\"report\":{}}", "application/json");
});

app.MapGet("/banned_urls", () =>
{
    string? stored = state.ReadBannedUrlsJson();
    return Results.Content(stored ?? "{\"banned\":{}}", "application/json");
});

app.MapPost("/telemetry", (HttpContext context, TelemetryRequest? request) =>
{
    state.TouchUser(request?.UserId ?? string.Empty);
    friends.RecordIdentity(ClientKey.Resolve(context), request?.UserId, request?.Identity);
    return Results.NoContent();
});

app.MapPost("/syncdata", (SyncRequest? request) =>
{
    int players = request?.Data?.Count ?? 0;
    state.RecordRoom(request?.Directory ?? string.Empty, request?.Region ?? string.Empty, players);
    return Results.NoContent();
});

app.MapPost("/reportban", (BanRequest? request) =>
{
    state.RecordBanReport(new BanReport(
        DateTimeOffset.UtcNow.ToString("o"),
        Truncate(request?.Error, 512) ?? string.Empty,
        Truncate(request?.Version, 64) ?? string.Empty,
        request?.Data is { Count: > 0 } mods
            ? mods.Select(mod => Truncate(mod, 128) ?? string.Empty).ToList()
            : new List<string>()));

    return Results.NoContent();
});

app.MapGet("/getfriends", (HttpContext context) =>
{
    string? userId = friends.ResolveUser(ClientKey.Resolve(context));
    if (userId is null)
        return Results.Json(new { error = "unknown identity, send telemetry first" }, statusCode: StatusCodes.Status401Unauthorized);

    return Results.Ok(friends.GetSnapshot(userId));
});

app.MapPost("/frienduser", (HttpContext context, UidRequest? request) =>
{
    string? userId = friends.ResolveUser(ClientKey.Resolve(context));
    if (userId is null)
        return Results.Json(new { error = "unknown identity, send telemetry first" }, statusCode: StatusCodes.Status401Unauthorized);

    string target = request?.Uid ?? string.Empty;
    if (string.IsNullOrWhiteSpace(target))
        return Results.BadRequest(new { error = "uid is required" });

    string result = friends.SendOrAccept(userId, target);

    return result switch
    {
        "self" => Results.BadRequest(new { error = "cannot add yourself" }),
        "already-friends" => Results.Json(new { error = "already friends" }, statusCode: StatusCodes.Status409Conflict),
        _ => Results.Ok(new { status = result })
    };
});

app.MapPost("/unfrienduser", (HttpContext context, UidRequest? request) =>
{
    string? userId = friends.ResolveUser(ClientKey.Resolve(context));
    if (userId is null)
        return Results.Json(new { error = "unknown identity, send telemetry first" }, statusCode: StatusCodes.Status401Unauthorized);

    string target = request?.Uid ?? string.Empty;
    if (string.IsNullOrWhiteSpace(target))
        return Results.BadRequest(new { error = "uid is required" });

    string result = friends.RemoveRelation(userId, target);

    return result == "no-relation"
        ? Results.Json(new { error = "no such friend or request" }, statusCode: StatusCodes.Status404NotFound)
        : Results.Ok(new { status = result });
});

app.MapFallback(() => Results.NotFound(new { error = "not found" }));

app.Run();

static string? Truncate(string? value, int maxLength) =>
    value is null ? null : value.Length <= maxLength ? value : value[..maxLength];

public static class ClientKey
{
    public static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && forwarded.Count > 0)
        {
            string raw = forwarded[0] ?? string.Empty;
            int comma = raw.IndexOf(',');
            string candidate = (comma > 0 ? raw[..comma] : raw).Trim();

            if (!string.IsNullOrEmpty(candidate))
                return candidate;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public sealed record VoteRequest([property: JsonPropertyName("option")] string? Option);

public sealed record UidRequest([property: JsonPropertyName("uid")] string? Uid);

public sealed record TelemetryRequest(
    [property: JsonPropertyName("userid")] string? UserId,
    [property: JsonPropertyName("identity")] string? Identity,
    [property: JsonPropertyName("region")] string? Region,
    [property: JsonPropertyName("playerCount")] int? PlayerCount);

public sealed record SyncRequest(
    [property: JsonPropertyName("directory")] string? Directory,
    [property: JsonPropertyName("region")] string? Region,
    [property: JsonPropertyName("data")] Dictionary<string, System.Text.Json.JsonElement>? Data);

public sealed record BanRequest(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("data")] List<string>? Data);

public sealed class PollRefreshService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly ServerState _state;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<PollRefreshService> _logger;

    public PollRefreshService(ServerState state, IHttpClientFactory factory, ILogger<PollRefreshService> logger)
    {
        _state = state;
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _state.RefreshPollAsync(_factory.CreateClient(), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError("Poll refresh failed: {Message}", ex.Message);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
