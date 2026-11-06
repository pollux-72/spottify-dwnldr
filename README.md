# spottify-dwnldr

Request songs in a chat, get them downloaded from your own Usenet provider, and stream the resulting library yourself.

The planned flow is: Telegram bot, then a core service that resolves the album through MusicBrainz, searches your Newznab indexer, hands the NZB to SABnzbd, imports and tags the files, and finally serves them with Navidrome to an Android app.
Work is tracked in `backlog/` (use the [Backlog.md](https://github.com/MrLesk/Backlog.md) CLI).

## Status

Only the project scaffold exists so far (TASK-2): the core service starts, creates its SQLite database and answers `/healthz`.
The metadata, indexer, downloader and import steps are defined as interfaces in `Spottify.Core` and implemented by later tasks.

## Layout

| Project | Purpose |
|---|---|
| `src/Spottify.Core` | Domain model (`DownloadRequest`, `RequestStatus`) and the interfaces `IMetadataProvider`, `IIndexer`, `IDownloader`, `ILibraryImporter` |
| `src/Spottify.Infrastructure` | SQLite persistence with EF Core and its migrations |
| `src/Spottify.Host` | ASP.NET Core host: configuration, startup, health endpoint |
| `tests/Spottify.Tests` | xUnit tests, including a test that starts the real host |

## Build, test, run

Requires the .NET 10 SDK.

```bash
dotnet build SpottifyDwnldr.slnx
dotnet test SpottifyDwnldr.slnx
dotnet run --project src/Spottify.Host     # state goes to ./data, health check at /healthz
```

## Configuration

Settings come from `appsettings.json` and from environment variables with the prefix `SPOTTIFY_`.
Nested keys use a double underscore.

| Variable | Default | Meaning |
|---|---|---|
| `SPOTTIFY_Spottify__DataDirectory` | `data` (`/data` in the image) | Folder for persistent state |
| `SPOTTIFY_Spottify__DatabaseFileName` | `spottify.db` | SQLite file name (no directories) inside the data directory |
| `SPOTTIFY_Spottify__MusicBrainz__UserAgent` | `SpottifyDwnldr/0.1 (https://github.com/pollux-72/spottify-dwnldr)` | Sent to MusicBrainz, which blocks anonymous clients. Use `Name/Version (contact)` |
| `SPOTTIFY_Spottify__MusicBrainz__MinRequestInterval` | `00:00:01.1` | Minimum spacing between requests (MusicBrainz allows about one per second) |
| `SPOTTIFY_Spottify__MusicBrainz__MaxRetries` | `3` | Retries after a 429 or 5xx answer |
| `SPOTTIFY_Spottify__MusicBrainz__MaxCandidates` | `8` | Albums returned per search |

Secrets such as API keys and tokens will only be read from the environment and must never be committed.

## Song lookup (MusicBrainz)

`IMetadataProvider` turns a request such as `Daft Punk - One More Time` into albums and a track list.
It searches official studio albums first and falls back to a wider search, ranks albums by the requested artist, then match score, then type (studio album before single, compilation, live), and always returns a list instead of choosing for you.
Without an `Artist - ` prefix only the title is searched, which finds covers first, so chat clients should ask for both.

The tests use trimmed real MusicBrainz responses.
To check the real service once (about ten requests), run:

```bash
SPOTTIFY_LIVE_TESTS=1 dotnet test --filter FullyQualifiedName~Live
```

## Docker

```bash
docker build -t spottify-dwnldr .
docker run -d -p 8080:8080 -v spottify-data:/data spottify-dwnldr
curl http://localhost:8080/healthz
```

The image runs as a non-root user and keeps all state in the `/data` volume.

## Database migrations

Migrations are applied automatically at startup. To add one after changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Spottify.Infrastructure --startup-project src/Spottify.Host --output-dir Persistence/Migrations
```

## License

See [LICENSE](LICENSE).
