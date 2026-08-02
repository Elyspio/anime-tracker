# AGENTS.md

Guidance for coding agents working in this repository.

## Product

Anime Tracker scrapes the seasonal anime listing from nautiljon.com into MongoDB and answers one
question per show: **when will every episode be out?** The grid is sorted by how soon each anime
becomes bingeable, so a season can be triaged into "watch now", "wait N weeks", and "no end
announced". Score, popularity and genres exist only to pick between shows that are equally ready.

There is no user state — no watchlist, no per-episode progress. Browsing is anonymous; the only
privileged action is spending the scraping budget.

See [CONTEXT.md](CONTEXT.md) for the domain vocabulary.

## Repository map

```text
back/
  AnimeTracker.Abstractions/       Models, transports, ports. Depends on nothing.
  AnimeTracker.Core/               AnimeService, BingePredictor, AnimeRefreshJob, AnimeAssembler.
  AnimeTracker.Adapters.Nautijon/  Only project that knows nautiljon.com HTML and FlareSolverr.
  AnimeTracker.Adapters.MongoDB/   Repositories, BSON conventions.
  AnimeTracker.Adapters.Hangfire/  Recurring-job scheduling, Mongo storage.
  AnimeTracker.Web/                API, auth, composition root, static SPA hosting.
  AnimeTracker.AppHost/            Aspire: MongoDB, Keycloak, FlareSolverr, API, Vite.
front/
  src/config/    Runtime config, theme, view-mode preference
  src/core/api/  Axios client, TanStack Query hooks, hand-written API types
  src/core/      binge.ts — countdown formatting and filtering
  src/view/      Layout and the two season views
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
- The cadence is the **median** interval between **distinct** release dates. Median absorbs a
  pause week that a mean would smear across the whole estimate; distinct dates stop a double
  episode from registering as a zero-day gap and collapsing the countdown.
- An anime whose total episode count was never announced gets `UnknownEnd` and **no date**.
  Inventing a plausible 12-episode default would make the countdown untrustworthy everywhere,
  because nothing on screen would distinguish a measurement from a guess.
- Only episodes whose release date has passed count as released. The stored list mixes aired and
  scheduled episodes.
- Success green is reserved for "bingeable" in the UI. Nothing else may use it.

### Scraping

- Nautiljon sits behind Cloudflare. Every request goes through **FlareSolverr**
  (`Nautijon:FlareSolverrUrl`), which holds the clearance cookie. Aspire runs one locally; a
  deployment must point at an external instance. Nothing works without it.
- A season refresh is a **sequential** walk with a delay between animes, and the Hangfire server
  runs a single worker. Parallelising it would multiply the request rate against a site that is
  being scraped on sufferance, and would not finish sooner behind a single-threaded solver.
- Consequently a refresh takes tens of minutes. `POST /api/animes/refresh` queues the job and
  answers 202; it never scrapes inline. Any caller that waits for completion is a bug.
- The Nautijon client carries **no resilience pipeline**, and `AddHostingDefaults` deliberately
  does not put one on every client. A 30s attempt timeout is shorter than a Cloudflare challenge
  takes to solve, and an automatic retry doubles the load on the site. Back-off belongs to
  `ClientSideRateLimitedHandler`, which reacts to 429 specifically.
- `AnimeRepository.Refresh` preserves the episodes already stored when replacing a season: the
  listing page does not carry them, and they are fetched anime by anime afterwards.
- Tests never reach the network or a solver. Drive the adapter through `FakeHttpMessageHandler`.

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

- `GET /api/animes` is `[AllowAnonymous]`: the data is a public broadcast schedule. Everything that
  mutates carries `[Authorize(AuthModule.AdminPolicy)]`. There is **no default policy** — unlike the
  reference application, adding an endpoint leaves it anonymous, so new mutating routes must opt in
  explicitly.
- Keycloak nests realm roles under `realm_access.roles`; `AuthModule` flattens them into role claims.
- The frontend does not decode the token to decide what to render. It calls the API and renders the
  refusal, so the required role has a single definition.
- The Hangfire dashboard is browser-navigated and a bearer token cannot reach it. It is mapped in
  Development only. Exposing it in production requires a cookie/OIDC scheme, not an exception.

## Development

```bash
aspire run                                  # Mongo + Keycloak + FlareSolverr + API + Vite
dotnet build back/AnimeTracker.slnx
dotnet test back/AnimeTracker.slnx
```

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

- Prediction or cadence changes: extend `AnimeTracker.Core.Tests`.
- Nautiljon parsing changes: extend `AnimeTracker.Adapters.Tests`. Two layers matter and both must
  stay: unit tests over hand-written markup pin the parsing *rules*, and the recorded pages under
  `Fixtures/` pin the *markup* — they are the only thing that catches a layout change upstream.
  Re-record a fixture by fetching the page through FlareSolverr rather than editing it by hand.
- Match selectors on a single class token (`contains(concat(' ', normalize-space(@class), ' '), ' x ')`),
  never on the whole `@class`. The site appends presentational classes without notice; an exact
  match on `genres tagsList` is what a `genres_scrollable` suffix turned into a crash on every scrape.
- Nautiljon dates are `dd/MM/yyyy` and its numbers sit next to private-use icon glyphs. Parse dates
  with an explicit format list, and reduce a number to its digits before parsing — never rely on the
  ambient culture, which is the invariant one inside a container.
- Countdown formatting or filtering changes: extend `front/src/core/binge.test.ts`.
- The `Season` and `BingeStatus` names are a contract between `JsonStringEnumConverter` and the
  hand-written TypeScript unions in `front/src/core/api/types.ts`. Both sides have tests; keep them.

## Code style

- `.editorconfig` is authoritative: UTF-8, LF, final newline, tabs at width 4. YAML, Markdown and
  JSON use spaces.
- C# has nullable reference types and implicit usings enabled. Follow the existing file-scoped
  namespaces, primary constructors, records, and cancellation-token propagation.
- MUI 9 removed system props: layout goes in `sx`, not as direct props on `Stack`/`Typography`.
- This project's namespace ends in `.MongoDB`, which shadows the driver's `MongoDB.*` namespaces.
  Import the nested namespace rather than writing a fully qualified type.
- Comments explain concurrency, scraping etiquette or design constraints — not what the code says.
- Product UI text is French; code, identifiers and comments are English.

## Deployment cautions

`deploy/build/build.ps1` builds, pushes to an external registry and deploys an external Helm chart.
Run it only when explicitly asked to publish. Run a **single replica**: the Hangfire server and the
scraper are singletons. Production must also supply `Nautijon:FlareSolverrUrl` pointing at a
FlareSolverr instance outside this container.
