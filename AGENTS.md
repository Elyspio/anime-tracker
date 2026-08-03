# AGENTS.md

Guidance for coding agents working in this repository.

## Product

Anime Tracker pulls the seasonal anime listing from AniList into MongoDB and answers one question
per show: **when will every episode be out?** Every card carries that countdown, so a season can be
triaged into "watch now", "wait N weeks", and "no end announced" — and sorting by soonest bingeable
is one click away. The grid opens on the most-rated shows first, because a season nobody has heard
of is triaged by reputation before it is triaged by date. Score, vote count, popularity, format and
genres exist to pick between shows, never to replace the countdown.

There is no user state — no watchlist, no per-episode progress. Browsing is anonymous; the only
privileged action is triggering a refresh.

Product text is English. These documents are French.

See [CONTEXT.md](CONTEXT.md) for the domain vocabulary.

## Repository map

```text
back/
  AnimeTracker.Abstractions/       Models, transports, ports. Depends on nothing.
  AnimeTracker.Core/               AnimeService, BingePredictor, AnimeRefreshJob, assemblers.
  AnimeTracker.Adapters.AniList/   Only project that knows AniList's GraphQL schema.
  AnimeTracker.Adapters.MongoDB/   Repositories, BSON conventions.
  AnimeTracker.Adapters.Hangfire/  Recurring-job scheduling, Mongo storage.
  AnimeTracker.Web/                API, auth, composition root, static SPA hosting.
  AnimeTracker.AppHost/            Aspire: MongoDB, Keycloak, API, Vite.
front/
  src/config/    Runtime config, theme, view-mode preference
  src/core/api/  Axios client, TanStack Query hooks, hand-written API types
  src/core/      binge.ts — countdown formatting and filtering; ranking.ts — sort, thresholds,
                 format and adult filters; refreshRuns.ts — run status labels and duration
  src/view/      Layout, the refresh-runs drawer, and the two season views
deploy/build/    Single-container image and deployment script
```

Backend: .NET 10, ASP.NET Core, Aspire 13, MongoDB, Hangfire, xUnit v3.
Frontend: React 19, TypeScript, Vite+, MUI 9, TanStack Query, Axios, `react-oidc-context`, Vitest.

## Invariants

These are the properties the design rests on. Changing them is a product decision, not a refactor.

### The prediction

- `BingePredictor` is pure and static, and its result is **computed on every read, never stored**.
  It is a function of the episode list plus today's date; a stored copy would be stale the next
  morning and wrong again after any change to the algorithm.
- When the schedule reaches the last announced episode, that date is **`Announced`** — the
  broadcaster's, not ours. The distinction between a measurement and a guess is one the product has
  always claimed on screen and could not make until the source began publishing future dates; do not
  collapse `Announced` back into `Estimated`.
- The cadence is the **median** interval between **distinct** release dates, and now only fills in
  the tail a schedule does not cover. Median absorbs a pause week that a mean would smear across the
  whole estimate; distinct dates stop a double episode from registering as a zero-day gap.
- An anime whose total episode count was never announced gets `UnknownEnd` and **no date**, and so
  does one with a total but no published slot at all. Inventing a plausible 12-episode default would
  make the countdown untrustworthy everywhere, because nothing on screen would distinguish a
  measurement from a guess.
- Only episodes whose release date has passed count as released. The stored list mixes aired and
  scheduled episodes.
- Success green is reserved for "bingeable" in the UI. Nothing else may use it.

### The source

- Anime data comes from **AniList's public GraphQL API** (`AniList:Endpoint`). No credentials, no
  solver, no browser: a season is one paginated query that returns everything — scores, vote
  histogram, popularity, studios, genres, format, adult flag and the **airing schedule, future
  dates included**. See [ADR 0002](docs/adr/0002-anilist-replaces-nautiljon.md) for why the previous
  source was abandoned rather than worked around.
- **Page on `hasNextPage` and nothing else.** `pageInfo.total` is capped at 5000 for every query, so
  treating it as a count pages far past the end of a season. `AniList:MaxPages` only guards against
  a source that never says it has finished.
- Episodes are dated by their **Japanese broadcast day**, not by UTC. AniList publishes each slot as
  a Unix timestamp and late-night slots — 01:30 on a Saturday is an entire genre's habit — fall on
  the previous day when read as UTC. JST has no daylight saving, so the offset is a constant.
- An anime's identity is its **AniList id**, never its title or URL.
- A refresh is one fetch and lands in about a second. It still goes through the queue:
  `POST /api/animes/refresh` answers 202, which is what gives the button and the nightly job a single
  path and leaves a written record of every run. A season already refreshing comes back as **409**
  carrying that run.
- **No automatic retry.** `GlobalJobFilters` sets `AutomaticRetry` to zero attempts. A refresh is
  idempotent and cheap to trigger again by hand; ten silent replays of a failing job are not
  something anyone asked for.
- The AniList client carries **no resilience pipeline**, and `AddHostingDefaults` deliberately does
  not put one on every client. The API allows 30 requests a minute and a season costs two; the only
  failure worth special handling is a 429, whose `Retry-After` the client obeys literally.
- GraphQL reports failure **inside a 200**. Reading only the status code would store an empty season
  and call it a success — `AniListClient` inspects the `errors` array.
- `AnimeRepository.Refresh` replaces a season wholesale. One fetch carries every field an anime has,
  so there is nothing stored worth merging in; only the document id survives.
- Tests never reach the network. Drive the adapter through `FakeHttpMessageHandler`.

### Scheduling and storage

- Development runs MongoDB as a **one-member replica set**, built from
  `AnimeTracker.AppHost/mongo/Dockerfile`, so change streams and transactions behave as they do in
  a real deployment. The image bakes the internal-auth keyfile mongod demands of any replica set
  running with authentication, and the entrypoint initiates the set once the server is up — the
  stock entrypoint strips `--replSet` for its own bootstrap, so it cannot do this itself.
- Development credentials are fixed at `aspire`/`aspire` and committed on purpose. That is a
  loopback container holding a public broadcast schedule; deployments supply their own connection
  string and never read these.
- The Mongo host port is pinned to 27017 because the set advertises itself as `localhost:27017`.
  On a random host port the driver would discover that address and dial a port nothing serves.
- Hangfire nonetheless uses `CheckQueuedJobsStrategy.TailNotificationsCollection` rather than its
  default change-stream watcher. Both are instant; this one costs the app no dependency on the
  deployment's topology, so pointing it at a standalone MongoDB stays a supported configuration.
- `MongoMappings.Register()` owns every process-wide BSON convention, and runs before any collection
  is resolved. Enums are stored **as names**, so reordering one cannot reinterpret stored documents.
  Guids get an **explicit `Standard` representation**: driver 3.x has no default and throws rather
  than guessing, because the legacy layouts byte-order the value differently and picking wrong is
  silent corruption. Never put a BSON attribute on a `Models/Base` type to work around this — those
  types are shared with the transports.

### Layering

- Controllers talk to services only (`IAnimeService`). A controller never injects a repository or
  an adapter.
- Transports live in `Abstractions/Models/Transports`, entities in `Models/Entities`, base models
  in `Models/Base`. Interfaces live in `Abstractions/Interfaces`, implementations in Core.
- One `Add*` extension per project, wired in `Web/Program.cs` and nowhere else.

### Tracing

- Every behavioural class inherits its `Elyspio.Utils.Telemetry` base: controllers →
  `TracingController`, services → `TracingService`, adapters → `TracingAdapter`, repositories →
  `TracingRepository`. Static classes (`BingePredictor`, `AnimeAssembler`) and the hosted service
  are exempt — they hold no behaviour worth a span.
- Every public method opens `using var trace = LogX($"{Log.F(arg)}")`. Private helpers and pure
  mappings do not.
- Telemetry is bootstrapped in `Web/Program.cs` via `AppOpenTelemetryBuilder` +
  `UseSerilogWithTelemetry`; each product assembly is registered with `AddAssembly<T>`. There is no
  ServiceDefaults project; health checks, service discovery and HTTP resilience live in
  `Web/Hosting/HostingModule.cs`.

### Auth

- `GET /api/animes` and `GET /api/animes/refreshes` are `[AllowAnonymous]`: the data is a public
  broadcast schedule, and whether a refresh has ever succeeded explains an empty grid to whoever is
  looking at one. Everything that mutates carries `[Authorize(AuthModule.AdminPolicy)]`. There is
  **no default policy** — unlike the reference application, adding an endpoint leaves it anonymous,
  so new mutating routes must opt in explicitly.
- A refresh run is served anonymously, so its `Error` carries `exception.Message` and never a stack
  trace.
- Keycloak nests realm roles under `realm_access.roles`; `AuthModule` flattens them into role claims.
- The frontend does not decode the token to decide what to render. It calls the API and renders the
  refusal, so the required role has a single definition.
- The Hangfire dashboard is browser-navigated and a bearer token cannot reach it. It is mapped in
  Development only. Exposing it in production requires a cookie/OIDC scheme, not an exception.

## Development

```bash
aspire run                                  # Mongo + Keycloak + API + Vite
dotnet build back/AnimeTracker.slnx
dotnet test back/AnimeTracker.slnx
```

### When the build cannot write its output

A running AppHost holds `AnimeTracker.Web`'s assemblies open, so `dotnet build` fails with
`MSB3021 … being used by another process` — no compile error, just a locked file. **Stop the
AppHost and start it again rather than working around it**: building into a scratch directory
proves the code compiles but leaves the running app on the old binaries, which is how a fix gets
declared done while the process under test never received it.

```bash
aspire ps                                   # which AppHosts are running
aspire stop                                 # this one; --all for every AppHost
aspire run
```

Restarting is also the only way a backend change reaches the running app: the API is not
hot-reloaded. `aspire start` runs the same thing detached, and `aspire logs` reads a resource's
output without attaching to it.

```bash
cd front
pnpm install --frozen-lockfile
pnpm check    # `vp check --fix` mutates files; review the diff
pnpm test
pnpm build
```

Seed accounts in the local realm: `admin`/`admin` holds `anime-tracker-admin`; `user`/`user` holds
nothing, which is how the 403 path stays exercised.

**First run starts with an empty database.** The grid renders its empty state until someone signs in
as `admin` and triggers a refresh — that button is the bootstrap path.

## Testing expectations

- **Assertions use Shouldly, doubles use NSubstitute.** No `Assert.*`, no hand-rolled stubs for
  interfaces — `FakeHttpMessageHandler` stays because `HttpMessageHandler.SendAsync` is protected
  and cannot be substituted.
- Prediction or cadence changes: extend `AnimeTracker.Core.Tests`. Refresh choreography — who is
  asked, in what order, what the run ends up saying — lives in `AnimeServiceTests`, every port
  substituted.
- Anything stored: extend `MongoMappingsTests`. It round-trips the entities through BSON with no
  server, and it is the layer that had no test until a run failed to insert in production.
- AniList mapping changes: extend `AnimeTracker.Adapters.Tests`. Two layers matter and both must
  stay: `MediaAssemblerTests` pins the mapping *rules* over hand-written nodes, and the recorded
  replies under `Fixtures/` pin the API's *shape* — they are the only thing that catches a schema
  change upstream. Re-record a fixture by replaying the adapter's own query, never by editing it.
- A fixture cannot express a field AniList has never returned, so the rules layer is where nulls,
  unknown formats and empty nodes are covered. Every one of them appears in a real season.
- Countdown formatting or filtering changes: extend `front/src/core/binge.test.ts`. Sort or
  threshold changes: `ranking.test.ts`. Run progress or duration: `refreshRuns.test.ts`.
- The `AnimeSeason`, `BingeStatus` and `RefreshStatus` names are a contract between
  `JsonStringEnumConverter` and the hand-written TypeScript unions in
  `front/src/core/api/types.ts`. Both halves of the tripwire exist and must be edited together:
  `EnumContractTests` in `AnimeTracker.Core.Tests`, and `types.test.ts` on the frontend, where each
  union is declared once as a `Record<Union, true>` so the compiler catches a forgotten member.
- The **vote count** (`VotesCount`, from `<meta itemprop="ratingCount">` on the anime's own page) and
  **popularity** (the listing's "N membres veulent le voir / l'ont vu") are different numbers. Only
  the first says how many people graded the show. Do not use one where the other is meant.

## Code style

- `.editorconfig` is authoritative: UTF-8, LF, final newline, tabs at width 4. YAML, Markdown and
  JSON use spaces.
- C# has nullable reference types and implicit usings enabled. Follow the existing file-scoped
  namespaces, primary constructors, records, and cancellation-token propagation.
- MUI 9 removed system props: layout goes in `sx`, not as direct props on `Stack`/`Typography`.
- This project's namespace ends in `.MongoDB`, which shadows the driver's `MongoDB.*` namespaces.
  Import the nested namespace rather than writing a fully qualified type.
- Comments explain concurrency, source quirks or design constraints — not what the code says.
- Product UI text is French; code, identifiers and comments are English.

## Deployment cautions

`deploy/build/build.ps1` builds, pushes to an external registry and deploys an external Helm chart.
Run it only when explicitly asked to publish. Run a **single replica**: the Hangfire server and the
refresher are singletons. The only external dependency at runtime is an outbound HTTPS call to
AniList; there is no solver or proxy to deploy alongside it.
