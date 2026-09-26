# Seralyth Menu Server

Backend for Seralyth Menu. Replaces `https://menu.seralyth.software`, which is currently
returning HTTP 530 (the origin behind Cloudflare is down), which breaks poll voting,
the online-user counter, and report data.

The poll question itself is **not** stored here. It is mirrored from
`data.json` in the `1x1x1x1736/api` repository, the same file the menu already reads.
Change the poll on GitHub and the tally resets here automatically.

## Endpoints

| Method | Route          | Body                                              | Response |
| ------ | -------------- | ------------------------------------------------- | -------- |
| GET    | `/health`      | –                                                 | `{ ok, poll, optionA, optionB, votes, users }` |
| POST   | `/vote`        | `{ "option": "a-votes" \| "b-votes" }`            | `{ "votes": { "a": N, "b": N }, ... }` |
| GET    | `/usercount`   | –                                                 | `{ "mods": { "seralyth": { "users": N } } }` |
| GET    | `/reportdata`  | –                                                 | `{ "report": { ... } }` |
| POST   | `/telemetry`   | `{ userid, identity, region, playerCount, ... }`  | `204` |
| POST   | `/syncdata`    | `{ directory, region, data }`                     | `204` |
| POST   | `/reportban`   | `{ error, version, data: [] }`                   | `204` |
| GET    | `/getfriends`  | –                                                 | `{ friends, incoming, outgoing }` |
| POST   | `/frienduser`  | `{ "uid": "..." }`                                | `{ status }` |
| POST   | `/unfrienduser`| `{ "uid": "..." }`                                | `{ status }` |
| WS     | `/`            | see below                                         | relayed JSON |

`/vote` is rate limited per client IP (default 5 per minute) and returns `429` past that.

## Identity

The client sends no auth token, so the server identifies you by IP, learned from
`/telemetry` (which carries `userid` and `identity`). Behind Cloudflare the first
`X-Forwarded-For` entry is used. The IP → userid map lives in memory, so after a
server restart every client must send telemetry again before the friends API
responds — the client does this on a timer, so it heals on its own.

Friend names come from telemetry too, so a name shows as blank until that user has
checked in at least once.

## Friends

`/getfriends` returns the three dictionaries the menu expects:

```json
{
  "friends":  { "USERID": { "online": true, "currentRoom": "MyRoom", "currentName": "Bob", "currentUserID": "USERID" } },
  "incoming": { "USERID": { "currentName": "Bob", "currentUserID": "USERID" } },
  "outgoing": { "USERID": { "currentName": "Bob", "currentUserID": "USERID" } }
}
```

The menu drives four different actions through only two endpoints, so the server
picks the operation from the current relationship state:

| Endpoint     | State                        | Result |
| ------------ | ---------------------------- | ------ |
| `/frienduser`| you have an incoming request | accept it, both sides become friends |
| `/frienduser`| otherwise                    | send a request (lands in their `incoming`) |
| `/unfrienduser` | you are friends           | remove the friendship |
| `/unfrienduser` | you have an incoming request | deny it |
| `/unfrienduser` | you have an outgoing request | cancel it |

Both sides of a request are always cleared together, so a denied or cancelled
request disappears from both players' lists.

`online` is true while that user holds an open friend socket. `currentRoom` is only
knowable when the user sends an invite, because the protocol has no room-presence
message — it is blank otherwise.

Errors return non-2xx with `{ "error": "..." }`, which is what the menu reads for
its failure notification: `400` bad or self target, `401` unknown identity, `404`
no such relation, `409` already friends.

## Friend socket

Connect to `wss://<host>/?mod=<menu name>`. The `mod` query is only logged.

Client → server (all carry `command` and `target`):

| Command       | Extra field   | Relayed to target as |
| ------------- | ------------- | -------------------- |
| `invite`      | `room`        | `to` = room name     |
| `reqinvite`   | –             | `command` only       |
| `preferences` | `preferences` | `data`               |
| `theme`       | `theme`       | `data`               |
| `macro`       | `macro`       | `data`               |
| `message`     | `message`, `color` | `message`, `color` |

Every relayed message carries `from` = sender user ID. Payloads are capped at
256 KB and unknown commands are ignored. The client ignores anything from a
non-friend, so the server relays without checking the friendship itself.


## Running locally

```
cd Server
dotnet run
```

It listens on `http://localhost:5000` unless `PORT` or `ASPNETCORE_URLS` is set.

Quick check:

```
curl http://localhost:5000/health
curl -X POST http://localhost:5000/vote -H "Content-Type: application/json" -d "{\"option\":\"a-votes\"}"
```

## Pointing the menu at it

Edit `ServerEndpoint` in `Classes/Menu/ServerData.cs` (line 51) to your deployed URL:

```csharp
public const string ServerEndpoint = "https://your-host.example";
```

`ServerDataEndpoint` stays on GitHub raw; it is unrelated to this server.

## Configuration

All optional, via environment variables.

| Variable                   | Default                    | Purpose |
| -------------------------- | -------------------------- | ------- |
| `PORT`                     | `5000`                     | Listen port |
| `DATA_DIR`                 | `bin/.../data`             | Where state files live |
| `DATA_URL`                 | `1x1x1x1736/api` `data.json` | Poll source |
| `VOTE_LIMIT_PER_MINUTE`    | `5`                        | Per-IP vote cap |
| `HEARTBEAT_TTL_MINUTES`    | `10`                       | How long a user counts toward `/usercount` |

## State files (inside `DATA_DIR`)

- `votes.json` – `{ poll, a, b, lastSync }`. Kept per poll question; a new question resets it.
- `report.json` – **you create this** to publish player reports. Format below.
- `banreports.json` – appended automatically from `/reportban`.

`report.json`:

```json
{
  "report": {
    "707651F6F1AD29F8": {
      "known-as": "SomePlayer",
      "reason": "Fly mod in ranked",
      "ButtonType": "Cheating",
      "actor": "reporter-name"
    }
  }
}
```

The key is the Gorilla user ID. `ButtonType` must be one of the values the game
understands: `Cheating`, `Toxicity`, `Mute`, `Cancel`. Anything unrecognised falls
back to `Cheating` in the client.

## Deploying

Any host that runs a .NET 9 container or build works. Set `PORT` if the platform
assigns one, and make sure state survives restarts by pointing `DATA_DIR` at a
persistent volume (Fly.io volume, Render disk, or a VPS path). Without a persistent
volume, votes reset on every deploy or restart.

HTTPS is required in production, since the menu requests `https://` URLs. Most hosts
terminate TLS for you; if you front it with Cloudflare, the per-IP rate limit reads
the first `X-Forwarded-For` entry.

## Notes

- No authentication on any endpoint. It is a public anonymous poll, and it only
  stores counts, a user ID heartbeat, and ban reports.
- `/usercount` is derived from `/telemetry` heartbeats, so the number is "users seen
  in the last 10 minutes", not a strict concurrent count.
- The client is tolerant of response shape, but this server matches the canonical
  `{"votes":{"a","b"}}` form exactly.
- `SeralythMenu.csproj` has `<Compile Remove="Server\**" />` so this project is not
  pulled into the netstandard2.1 menu build.
