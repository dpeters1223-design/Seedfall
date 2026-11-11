# Seedfall — Architecture

Draft 1, Nov 11, 2026. Owner: David Peters. This is the source of truth for how Seedfall is built. The controller reads it before writing any task spec, and a task that conflicts with it is wrong until this document changes.

## Fixed inputs

These came from David on Nov 11, 2026.

| Question | Decision |
| --- | --- |
| Scale for the first version | **Under 100 concurrent players.** One server, with seams to split later |
| Play style | **Hybrid.** The galaxy runs on orders and passing time. Combat and surface play happen in small live sessions |
| Universe time | **One shared compressed clock.** Starting value: 1 real day = 1 universe year |
| Server technology | **Plain .NET (C#) server**, sharing the game-logic library with the Unity client |

## Principles

1. **The server decides.** Clients send intentions ("send probe P-7 to Tau Ceti"). The server validates them, applies them and reports outcomes. Clients never change the universe directly.
2. **Nothing ticks unless someone is watching.** The universe is computed from time, not stepped frame by frame. A probe in flight is an equation, a mine is a rate, a factory is a finish time. The server does work only when an event comes due or someone asks a question. This is how 100 players with thousands of probes run on one modest box.
3. **Game rules live in one plain C# library.** It has no Unity and no networking. The server runs it, the client runs it for prediction and previews, and the tests run it in seconds with no Unity license.
4. **Same input, same output.** Randomness comes from seeds, and time comes from the universe clock, never the wall clock. Any bug can be replayed, and any test is repeatable.
5. **Build the simplest thing that keeps the seams.** One process and one database for now. The boundaries (star systems, sessions, foundries) are drawn so they can move to separate servers later without a rewrite.

## The big picture

```
            ┌──────────────────────── bigiron ─────────────────────────┐
 Unity      │  Universe server (.NET)                                  │
 client ◄──WebSocket──► API ─► Command handler ─► Seedfall.Sim (rules) │
 (player)   │                     │     ▲                              │
            │                     ▼     │                              │
            │              Event scheduler (future events by time)     │
            │                     │                                    │
            │                     ▼                                    │
            │              PostgreSQL (state + event log)              │
            │                                                          │
 Unity  ◄──UDP──► Session host (live combat / surface, later phase)    │
 clients    └──────────────────────────────────────────────────────────┘
```

## 1. Universe time

- **Universe Time (UT)** is a single number: seconds since the game's epoch, as a 64-bit integer, or a double where fractions matter.
- `UT(now) = UT_at_last_change + (real_now - real_at_last_change) × K`, where **K = 365.25** to start (1 real day = 1 universe year).
- K can change (for tuning, or events), but only by recording a new segment of the clock, so time never jumps backwards. Every component asks the **Clock service** for UT. Nothing reads the system clock directly.
- **What it feels like:** Alpha Centauri (4.4 ly) is about 5 real days away at 2G, Tau Ceti (11.9 ly) about 13 real days, and building a foundry over "real-world weeks" is decades of universe time.
- **Live sessions** (a dogfight, a surface drive) run on their own local seconds. In the universe the session occupies a short window of UT (a 10-minute fight is about 2.5 universe days), and the result is committed at the session's end time.

## 2. Motion: trips are equations

A probe's movement is stored as a **Trajectory**: origin, destination, departure UT and proper acceleration. Interstellar trips accelerate to the midpoint, flip, and decelerate. Position and velocity at any UT are computed in closed form, so nothing is ticked, ever.

Relativistic constant-acceleration motion, in units where c = 1, distances in light-years and time in years, with a = 2G ≈ 2.06 ly/yr²:

- Distance covered after coordinate time t from rest: `x(t) = (√(1 + (a·t)²) − 1) / a`
- Time to cover distance x from rest: `t(x) = √((1 + a·x)² − 1) / a`
- Probe's own elapsed time: `τ(x) = acosh(1 + a·x) / a`
- A trip of distance D takes `2·t(D/2)` in the universe frame and `2·τ(D/2)` aboard. Tau Ceti: about 12.8 years universe time, about 3.2 years aboard.

At departure the server computes the arrival UT and schedules a single **Arrival** event. Anyone asking "where is P-7?" gets `position(UT_now)`. The same function handles short in-system hops (the relativistic terms just vanish).

**Precision:** Unity's `Vector3` is 32-bit float and can't tell two points a few kilometers apart at interstellar distances. The core library uses its **own double-precision vector type** (`Vec3d`) and its own units:
- light-years between stars,
- astronomical units inside a system,
- meters locally.

The client converts to `Vector3` only for rendering, relative to the camera ("floating origin").

## 3. Everything else that changes over time: rates and events

Two patterns cover nearly all of the simulation.

- **Lazy accrual (rates).** A mine records `{amount_at, UT_at, rate, cap}` instead of adding ore every second. Current amount = `min(cap, amount_at + rate × (UT_now − UT_at))`. When something changes the rate (a probe leaves, an upgrade lands), the server first settles the amount up to that moment, then records the new rate.
- **Scheduled events.** Anything that happens at a known future time (arrival, a print job finishing, a foundry completing, a crisis starting) goes into the **event scheduler**: a table of `(due_UT, event)` plus an in-memory priority queue. The server loop pops due events in time order and applies them, and each may schedule more. On restart the queue is rebuilt from the table, so nothing is lost.

Combined with trajectories, this means a universe of thousands of probes costs almost nothing between events.

## 4. Commands: one door for every change

Every change to the universe is a **Command**: `TravelTo`, `Mine`, `Print`, `BuildFoundry`, `StoreBackup` and so on. A command:

1. arrives from a client, a probe's autonomous behavior, a crisis generator, or a test;
2. is **validated** against current state by pure functions in `Seedfall.Sim`;
3. is **applied** in one database transaction, which updates state, appends to the event log and schedules future events;
4. produces **outcome events** that are pushed to interested clients.

This one door gives us several things for free:
- **The UI and the console** (from the look survey) call the same commands.
- **Probe autonomy** is just server-side behavior that issues commands for the probe within its directive. "Going rogue" means the behavior ignores the directive.
- **Tests and the AI build loop** can drive the whole game as text.
- **An audit trail** for disputes and debugging.

## 5. Persistence

- **PostgreSQL**, on bigiron in Docker for now.
- **State tables** hold the current truth: players, probes, systems, foundries, caches, backups, the scheduler table and the clock segments.
- **An event log table** is append-only: every applied command and outcome, with UT and real time. It's used for audit, replay when debugging, and analytics. The log is *not* the source of truth for v1 (it isn't event sourcing). State tables are, which keeps queries and fixes simple.
- **Every command is one transaction.** A crash mid-command leaves nothing half-done. That matters for **foundry backups**: when a seed probe dies, it restores from the last *committed* backup, never a partial one.
- **Nightly database dumps** to disk. Later, also off-site.

## 6. Knowledge and what each client sees

Each player has a **knowledge base**: observations their probes made, each stamped with *when* it was observed. A client is sent:
- its own fleet,
- its knowledge base,
- public things (foundries, Earth broadcasts),
- live updates only for systems where it has a presence.

It never receives the whole universe. This keeps traffic small. It's also the game's fog of war, and it's literally what a **backup restores**: knowledge up to the backup time.

## 7. Communications: messages are physical

In-game communication (probe reports, directives, Earth broadcasts, crisis calls) is **not instant**, and how far and fast it goes is a **progression system** (see "Communication and travel ladder" in the design plan).

- A **Message** is a real object: sender, recipient, payload, send UT, and a computed **route** and **delivery UT**. Delivery is a scheduled event like any other (section 3).
- The **comm network is one shared, public graph** for the whole universe. Its fixed nodes are Earth, foundries, colonies and **relay stations**, and every player's messages can route through any of them, whoever built them. Probes are endpoints: they send and receive, but don't carry other players' traffic.
  - **Signal links** travel at c, with a maximum range set by the equipment tier. Delay is distance in light-years, in universe years.
  - **Relay stations** are built by players and open to all. The builder is recorded and rewarded (standing, and possibly a share of traffic credit, to be decided). Because they're shared infrastructure, attacking one hurts everyone, which should carry a prime directive and standing cost.
  - **Foundries require a connection.** A foundry can only be started in a system with a relay path back to Earth. So every foundry can reach Earth, new players spawning there start connected, and backups stored there stay reachable.
  - **Message contents stay private** to sender and recipient even though the route is public.
  - **Courier links:** with no signal path, a message can ride a probe physically, arriving with the courier's trajectory. Once FTL travel exists, this is the fastest way to carry news.
  - **Advanced links** (wormholes and the like) are just edges with other speeds and ranges.
- Routing is a shortest-delivery-time search over that graph. Unlocking a technology changes only edge types and ranges, so no other system changes.
- **Directives have the same delay.** An order to a probe takes effect when the message *arrives*. That's why probes need autonomy: out of contact, they act on their last directive. Rogue risk lives in that gap.
- **Knowledge (section 6) arrives by message.** What a player knows about a distant system is as old as the last message that reached them. A backup at a foundry holds whatever had reached that foundry.

## 8. Networking

- **Universe traffic** (commands, outcome events, knowledge updates) goes over **WebSocket** with JSON, which can move to a binary format later if needed. It's simple, works through firewalls, and is well supported on both ends.
- **Live sessions** (later phase) use UDP through Unity's networking stack. A session is a small instance: the universe server hands the participants and starting state to a **session host**, the session plays out, and it reports the result back as commands. Space combat can probably run in plain .NET. Multiplayer surface driving needs Unity physics, so it will likely need a headless Unity session host. That's deferred until Phase 4/6. Until then, surface play is single-player and reports its results.
- **Steam** handles login: the client gets a Steam session ticket, and the server verifies it with Steam. Universe state never lives on Steam.

## 9. World generation

- **Real stars** come from a public star catalogue (for example HYG; check its license before shipping). Their positions, spectral types and names are fixed.
- **Planets and everything below them** are generated **deterministically from the star's seed**, so any server can regenerate any system identically. Only player-made changes (mines, colonies, foundries) are stored.
- **AI-authored content** (Phase 8) is stored as *overrides* on top of a generated system, and is only ever added to systems that no player has discovered yet, so existing discoveries never change under anyone.
- **Each system's palette** (from the look survey) is part of its generated data.

## 10. Code layout

```
seedfall/
  src/
    Seedfall.Core/       math (Vec3d), units, Universe Time, IDs, seeded RNG     — netstandard2.1, no dependencies
    Seedfall.Sim/        game rules: trajectories, accrual, commands, validation — netstandard2.1, depends on Core
    Seedfall.Protocol/   messages between client and server                     — netstandard2.1
    Seedfall.Server/     host, WebSocket API, scheduler, PostgreSQL             — .NET (current LTS)
  tests/
    Seedfall.Core.Tests/ Seedfall.Sim.Tests/ Seedfall.Server.Tests/             — NUnit, run with `dotnet test`
  unity/                 the client. Uses Core/Sim/Protocol as prebuilt DLLs in Assets/Plugins/Seedfall
  tools/                 build scripts: build the libraries and copy the DLLs into Unity
```

- **netstandard2.1** is the target Unity can load, so the same compiled libraries run in both places.
- **Unity's own tests** cover only client things: rendering, input and UI.

## 11. Growing beyond one server

We don't build this for v1, but the seams are kept:

- **Star systems are the unit of ownership.** All state inside a system is changed by whoever owns that system. In v1 a single process owns all of them. Later, systems are spread across processes or machines, and an interstellar Arrival becomes a handoff message.
- **Foundries and player accounts are global** and live in a small shared service.
- **Sessions** are already separate instances by design.

## 12. What this changes in the build plan

- **Most tasks become plain .NET tasks**, tested with `dotnet test` in seconds and with no Unity license. That's much faster and simpler for the AI build loop. Unity tasks are for client work only.
- **T001 and T002 get rewritten.**
  - T001 becomes "Vec3d and Trajectory: position, velocity and arrival time at any UT" in `Seedfall.Core` and `Seedfall.Sim`, with double precision and no Unity types.
  - T002 becomes "Lazy resource accrual for mining".
  - Both get controller-written tests (see the build loop below).
- **The AI build loop** follows the revised shape agreed on Nov 11, 2026:
  - the controller writes each task's tests, checked to compile against a stub,
  - both local models implement independently,
  - feedback is one error at a time,
  - a task that's still failing goes to the controller instead of being blocked.
- **New setup needed on bigiron:** the .NET SDK, and Docker access for PostgreSQL (add `dpeters` to the `docker` group). Both need sudo.

## Open questions for David

1. **Comms range at the start.** How far does a starting probe's transmitter reach? A suggestion is about 5 ly: Alpha Centauri can report home, Tau Ceti can't without a relay. That makes the first relay chain an early, concrete goal.
   - Also to decide: what relay builders earn, and what happens to someone who destroys a relay.
2. **Clock factor.** Is 1 real day = 1 universe year the right feel? It's easy to tune later, but the first playtest should pick one.
3. **Are Earth crises shared or per player?** (Already an open question in the plan.) With one shared clock and physical messages, *shared* crises fall out naturally. Players hear about them when Earth's broadcast reaches them through their network, so far-flung or disconnected players hear late or not at all.
4. **Does a probe age?** Probe proper time (about 3.2 years to Tau Ceti versus 12.8 universe years) could drive wear and maintenance, or just be flavor text.

## Decision log

| # | Date | Decision | Why |
| --- | --- | --- | --- |
| 1 | 2026-11-11 | Plain .NET universe server with a shared rules library | Fast tests without Unity; light to run; the client reuses the same rules |
| 2 | 2026-11-11 | Event-driven simulation (trajectories, lazy accrual, scheduler), not per-frame ticks | Long set-and-forget trips and thousands of probes cost nothing between events |
| 3 | 2026-11-11 | One shared compressed clock, K = 365.25 to start | Trips of days or weeks, and one consistent universe for everyone |
| 4 | 2026-11-11 | Double-precision `Vec3d` in the core; Unity `Vector3` only for rendering | Float can't represent interstellar distances |
| 5 | 2026-11-11 | PostgreSQL state tables plus an append-only event log; one transaction per command | Crash-safe backups and caches; simple queries; full audit trail |
| 6 | 2026-11-11 | WebSocket + JSON for the universe; UDP live sessions later | Simple and reliable now; real-time networking only where needed |
| 7 | 2026-11-11 | Star systems are the unit of ownership | The natural line for splitting across servers later |
| 8 | 2026-11-11 | The controller writes tests; two local models implement independently | The critic role failed in T001 run 1; tests are acceptance criteria |
| 9 | 2026-11-11 | Messages are physical objects routed over one shared public comm graph; relays are open to all; a foundry needs a relay path to Earth | David's comms ladder (range-limited light-speed signals → relays → FTL travel → wormholes) is progression; shared relays reward cooperation; foundries are always connected |
