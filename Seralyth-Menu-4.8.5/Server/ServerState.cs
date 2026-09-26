using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seralyth.Server;

public sealed class ServerState
{
    public const string FallbackPoll = "What goes well with cheeseburgers?";
    public const string FallbackOptionA = "Fries";
    public const string FallbackOptionB = "Chips";

    private readonly string _dataDir;
    private readonly string _dataUrl;
    private readonly TimeSpan _heartbeatTtl;
    private readonly ILogger<ServerState> _logger;

    private readonly object _voteLock = new();
    private readonly object _banLock = new();
    private readonly ConcurrentDictionary<string, long> _heartbeats = new();
    private readonly List<BanReport> _banReports = new();
    private int _votesA;
    private int _votesB;

    public string PollKey { get; private set; } = FallbackPoll;
    public string OptionA { get; private set; } = FallbackOptionA;
    public string OptionB { get; private set; } = FallbackOptionB;
    public long LastPollSync { get; private set; }

    public ServerState(IConfiguration config, ILogger<ServerState> logger)
    {
        _logger = logger;
        _dataUrl = config["DATA_URL"]
            ?? "https://raw.githubusercontent.com/1x1x1x1736/api/refs/heads/main/data.json";
        _heartbeatTtl = TimeSpan.FromMinutes(
            double.TryParse(config["HEARTBEAT_TTL_MINUTES"], out var ttl) ? ttl : 10);

        var configuredDir = config["DATA_DIR"];
        _dataDir = string.IsNullOrWhiteSpace(configuredDir)
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : Path.GetFullPath(configuredDir);

        Directory.CreateDirectory(_dataDir);
        LoadVotes();
    }

    public string DataDirectory => _dataDir;
    public string DataUrl => _dataUrl;

    private string VotesFile => Path.Combine(_dataDir, "votes.json");
    private string ReportFile => Path.Combine(_dataDir, "report.json");
    private string BanReportFile => Path.Combine(_dataDir, "banreports.json");
    private string BannedUrlsFile => Path.Combine(_dataDir, "banned_urls.json");

    public VoteCounts GetCounts()
    {
        lock (_voteLock)
            return new VoteCounts(_votesA, _votesB);
    }

    public VoteCounts RecordVote(bool voteA)
    {
        lock (_voteLock)
        {
            if (voteA)
                _votesA++;
            else
                _votesB++;

            SaveVotes();
            return new VoteCounts(_votesA, _votesB);
        }
    }

    public void TouchUser(string userId)
    {
        if (!string.IsNullOrEmpty(userId))
            _heartbeats[userId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public int CountActiveUsers()
    {
        long cutoff = DateTimeOffset.UtcNow.Add(-_heartbeatTtl).ToUnixTimeMilliseconds();
        int total = 0;

        foreach (var pair in _heartbeats)
        {
            if (pair.Value < cutoff)
                _heartbeats.TryRemove(pair.Key, out _);
            else
                total++;
        }

        return total;
    }

    public void RecordRoom(string directory, string region, int playerCount)
    {
        _logger.LogInformation("Room {Directory} ({Region}) reported {Players} players", directory, region, playerCount);
    }

    public void RecordBanReport(BanReport report)
    {
        lock (_banLock)
        {
            _banReports.Add(report);
            if (_banReports.Count > 1000)
                _banReports.RemoveRange(0, _banReports.Count - 1000);

            WriteJson(BanReportFile, _banReports);
        }
    }

    public string? ReadReportJson()
    {
        try
        {
            if (!File.Exists(ReportFile))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(ReportFile), LenientDocumentOptions);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("report", out _)
                ? File.ReadAllText(ReportFile)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read report data: {Message}", ex.Message);
            return null;
        }
    }

    public string? ReadBannedUrlsJson()
    {
        try
        {
            if (!File.Exists(BannedUrlsFile))
                return null;

            using var document = JsonDocument.Parse(File.ReadAllText(BannedUrlsFile), LenientDocumentOptions);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("banned", out _)
                ? File.ReadAllText(BannedUrlsFile)
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read banned url list: {Message}", ex.Message);
            return null;
        }
    }

    public async Task RefreshPollAsync(HttpClient http, CancellationToken token)
    {
        try
        {
            using var response = await http.GetAsync(_dataUrl, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Poll source returned {Status}", (int)response.StatusCode);
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var document = await JsonDocument
                .ParseAsync(stream, LenientDocumentOptions, token)
                .ConfigureAwait(false);

            if (!document.RootElement.TryGetProperty("poll", out var pollElement))
                return;

            string question = pollElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(question))
                return;

            string optionA = document.RootElement.TryGetProperty("option-a", out var a) ? a.GetString() ?? FallbackOptionA : FallbackOptionA;
            string optionB = document.RootElement.TryGetProperty("option-b", out var b) ? b.GetString() ?? FallbackOptionB : FallbackOptionB;

            lock (_voteLock)
            {
                if (!string.Equals(question, PollKey, StringComparison.Ordinal))
                {
                    _logger.LogInformation("New poll detected, resetting tally: {Poll}", question);
                    PollKey = question;
                    _votesA = 0;
                    _votesB = 0;
                }

                OptionA = optionA;
                OptionB = optionB;
                LastPollSync = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                SaveVotes();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to refresh poll from {Url}: {Message}", _dataUrl, ex.Message);
        }
    }

    private void LoadVotes()
    {
        try
        {
            if (!File.Exists(VotesFile))
                return;

            using var document = JsonDocument.Parse(File.ReadAllText(VotesFile), LenientDocumentOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return;

            if (document.RootElement.TryGetProperty("poll", out var poll) &&
                poll.GetString() is string savedPoll &&
                !string.IsNullOrEmpty(savedPoll))
            {
                PollKey = savedPoll;
            }

            if (document.RootElement.TryGetProperty("a", out var a) && a.TryGetInt32(out int av))
                _votesA = av;
            if (document.RootElement.TryGetProperty("b", out var b) && b.TryGetInt32(out int bv))
                _votesB = bv;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load saved votes: {Message}", ex.Message);
        }
    }

    private void SaveVotes()
    {
        var payload = new
        {
            poll = PollKey,
            a = _votesA,
            b = _votesB,
            lastSync = LastPollSync
        };

        WriteJson(VotesFile, payload);
    }

    private void WriteJson<T>(string file, T value)
    {
        string temp = file + ".tmp";

        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(temp, file, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to write {File}: {Message}", Path.GetFileName(file), ex.Message);

            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly JsonDocumentOptions LenientDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };
}

public sealed record VoteCounts(
    [property: JsonPropertyName("a")] int A,
    [property: JsonPropertyName("b")] int B);

public sealed record BanReport(
    [property: JsonPropertyName("at")] string At,
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("mods")] IReadOnlyList<string> Mods);
