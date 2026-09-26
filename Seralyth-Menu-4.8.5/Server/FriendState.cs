using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seralyth.Server;

public sealed class FriendState
{
    private static readonly TimeSpan IdentityTtl = TimeSpan.FromMinutes(15);

    private readonly string _dataDir;
    private readonly ILogger<FriendState> _logger;
    private readonly object _lock = new();

    private readonly Dictionary<string, HashSet<string>> _friends = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _incoming = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _outgoing = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, string> _ipToUser = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string Name, long At)> _names = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _rooms = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, FriendSocket> _sockets = new(StringComparer.Ordinal);

    public FriendState(IConfiguration config, ILogger<FriendState> logger)
    {
        _logger = logger;
        string? configured = config["DATA_DIR"];
        _dataDir = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(_dataDir);
        Load();
    }

    private string FriendsFile => Path.Combine(_dataDir, "friends.json");

    public bool RecordIdentity(string clientKey, string? userId, string? identity)
    {
        if (string.IsNullOrWhiteSpace(clientKey) || string.IsNullOrWhiteSpace(userId))
            return false;

        string id = userId.Trim();
        _ipToUser[clientKey] = id;

        if (!string.IsNullOrWhiteSpace(identity))
            _names[id] = (identity.Trim(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        PruneNames();
        return true;
    }

    public string? ResolveUser(string clientKey)
    {
        if (string.IsNullOrEmpty(clientKey))
            return null;

        if (_ipToUser.TryGetValue(clientKey, out string? userId))
            return userId;

        return null;
    }

    public string GetName(string userId) =>
        _names.TryGetValue(userId, out var entry) ? entry.Name : string.Empty;

    // Name shown to clients. Falls back to the user id so a friend row is never blank,
    // even when the target has not sent telemetry recently (or their nickname contains
    // characters that were stripped before it reached the server).
    public string GetDisplayName(string userId)
    {
        string name = GetName(userId);
        return !string.IsNullOrWhiteSpace(name) ? name : ShortenId(userId);
    }

    private static string ShortenId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "Unknown";

        return userId.Length <= 12 ? userId : userId[..12];
    }

    public bool IsOnline(string userId) => _sockets.ContainsKey(userId);

    public string GetRoom(string userId) => _rooms.TryGetValue(userId, out string? room) ? room : string.Empty;

    public void SetRoom(string userId, string room)
    {
        if (!string.IsNullOrWhiteSpace(room))
            _rooms[userId] = room;
    }

    public void Register(FriendSocket socket, string userId) => _sockets[userId] = socket;

    public void Unregister(FriendSocket socket, string userId) =>
        _sockets.TryRemove(new KeyValuePair<string, FriendSocket>(userId, socket));

    public async Task<bool> SendToUserAsync(string userId, string json, CancellationToken token)
    {
        if (!_sockets.TryGetValue(userId, out FriendSocket? socket))
            return false;

        return await socket.SendAsync(json, token).ConfigureAwait(false);
    }

    public FriendSnapshot GetSnapshot(string userId)
    {
        lock (_lock)
        {
            var friends = new Dictionary<string, FriendView>(StringComparer.Ordinal);
            foreach (string id in Lookup(_friends, userId))
                friends[id] = new FriendView(IsOnline(id), GetRoom(id), GetDisplayName(id), id);

            var incoming = new Dictionary<string, PendingView>(StringComparer.Ordinal);
            foreach (string id in Lookup(_incoming, userId))
                incoming[id] = new PendingView(GetDisplayName(id), id);

            var outgoing = new Dictionary<string, PendingView>(StringComparer.Ordinal);
            foreach (string id in Lookup(_outgoing, userId))
                outgoing[id] = new PendingView(GetDisplayName(id), id);

            return new FriendSnapshot(friends, incoming, outgoing);
        }
    }

    public string SendOrAccept(string userId, string target)
    {
        lock (_lock)
        {
            if (string.Equals(userId, target, StringComparison.Ordinal))
                return "self";

            if (!Set(_friends, userId).Contains(target) && Incoming(userId).Remove(target))
            {
                Add(_friends, userId, target);
                Add(_friends, target, userId);
                Outgoing(userId).Remove(target);
                Outgoing(target).Remove(userId);
                Save();
                return "accepted";
            }

            if (Set(_friends, userId).Contains(target))
                return "already-friends";

            Outgoing(userId).Remove(target);
            Outgoing(userId).Add(target);
            Incoming(target).Add(userId);
            Save();
            return "sent";
        }
    }

    public string RemoveRelation(string userId, string target)
    {
        lock (_lock)
        {
            bool changed = false;

            if (Set(_friends, userId).Remove(target))
            {
                Set(_friends, target).Remove(userId);
                changed = true;
            }

            if (Incoming(userId).Remove(target))
            {
                Outgoing(target).Remove(userId);
                changed = true;
            }

            if (Outgoing(userId).Remove(target))
            {
                Incoming(target).Remove(userId);
                changed = true;
            }

            if (changed)
                Save();

            return changed ? "removed" : "no-relation";
        }
    }

    private static HashSet<string> Lookup(Dictionary<string, HashSet<string>> map, string key) =>
        map.TryGetValue(key, out HashSet<string>? set) ? set : new HashSet<string>(StringComparer.Ordinal);

    private static HashSet<string> Set(Dictionary<string, HashSet<string>> map, string key)
    {
        if (!map.TryGetValue(key, out HashSet<string>? set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            map[key] = set;
        }

        return set;
    }

    private HashSet<string> Incoming(string key) => Set(_incoming, key);
    private HashSet<string> Outgoing(string key) => Set(_outgoing, key);

    private static void Add(Dictionary<string, HashSet<string>> map, string key, string value) => Set(map, key).Add(value);

    private void PruneNames()
    {
        long cutoff = DateTimeOffset.UtcNow.Add(-IdentityTtl).ToUnixTimeMilliseconds();
        foreach (string id in _names.Where(pair => pair.Value.At < cutoff).Select(pair => pair.Key).ToList())
            _names.TryRemove(id, out _);
    }

    private void Save()
    {
        var payload = new
        {
            friends = _friends.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()),
            incoming = _incoming.ToDictionary(pair => pair.Key, pair => pair.Value.ToList()),
            outgoing = _outgoing.ToDictionary(pair => pair.Key, pair => pair.Value.ToList())
        };

        string temp = FriendsFile + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FriendsFile, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write friends.json: {Message}", ex.Message);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(FriendsFile))
                return;

            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(FriendsFile),
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            Read(document.RootElement, "friends", _friends);
            Read(document.RootElement, "incoming", _incoming);
            Read(document.RootElement, "outgoing", _outgoing);
            _logger.LogInformation("Loaded {Friends} friend relations", _friends.Values.Sum(set => set.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load friends.json: {Message}", ex.Message);
        }
    }

    private static void Read(JsonElement root, string name, Dictionary<string, HashSet<string>> map)
    {
        if (!root.TryGetProperty(name, out JsonElement element) || element.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
                continue;

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement item in property.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() is string value && !string.IsNullOrWhiteSpace(value))
                    set.Add(value);
            }

            if (set.Count > 0)
                map[property.Name] = set;
        }
    }
}

public sealed record FriendSnapshot(
    [property: JsonPropertyName("friends")] IReadOnlyDictionary<string, FriendView> Friends,
    [property: JsonPropertyName("incoming")] IReadOnlyDictionary<string, PendingView> Incoming,
    [property: JsonPropertyName("outgoing")] IReadOnlyDictionary<string, PendingView> Outgoing);

public sealed record FriendView(
    [property: JsonPropertyName("online")] bool Online,
    [property: JsonPropertyName("currentRoom")] string CurrentRoom,
    [property: JsonPropertyName("currentName")] string CurrentName,
    [property: JsonPropertyName("currentUserID")] string CurrentUserID);

public sealed record PendingView(
    [property: JsonPropertyName("currentName")] string CurrentName,
    [property: JsonPropertyName("currentUserID")] string CurrentUserID);
