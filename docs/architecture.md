# Seedfall — Architecture

Draft 2, Nov 11, 2026. Draft 2 folds in the external review and David's answers on foundry links, downtime and signal range. Owner: David Peters. This is the source of truth for how Seedfall is built. The controller reads it before writing any task spec, and a task that conflicts with it is wrong until this document changes.

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
3. **Game rules live in one plain C# library.** It has no Unity and no networking. The server runs it for real, the client runs it only for previews (advisory; the server's answer always wins), and the tests run it in seconds with no Unity license.
4. **Same input, same output.** Randomness comes from seeds, and time comes from the universe clock, never the wall clock. Any bug can be replayed, and any test is repeatable.
5. **Build for one server, and don't build for many.** One process and one database. The only concession to the future is free: all state is keyed by star system. Distributed anything waits until there's evidence it's needed.

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

- **Universe Time (UT)** is a 64-bit integer count of universe seconds since the game's epoch.
- **K = 365.25** to start: 1 real day = 1 universe year.
- **What it feels like:** Alpha Centauri (4.4 ly) is about 5 real days away at 2G, Tau Ceti (11.9 ly) about 13 real days, and building a foundry over "real-world weeks" is decades of universe time.

**The Clock service** is the only thing that knows UT. Nothing else reads the system clock. Its rules:

1. **It is persisted and only moves forward.** The database holds a checkpoint `(UT, real time, K)`, updated every few seconds while the server runs. While running, `UT(now) = UT_checkpoint + (real_now − real_checkpoint) × K`, never less than the last value handed out.
2. **The universe pauses while the server is down** (David's decision). On startup, the clock resumes from the last checkpoint's UT, not from the wall clock, so downtime simply doesn't exist in universe time. A crash loses at most the few universe-seconds since the last checkpoint, and no events fire in that gap because the event processor runs in the same process.
3. **Changing K** writes a new checkpoint. Events are scheduled in UT, so they stay where they are in universe time and simply arrive sooner or later in real time. No stored data is rewritten.
4. **Admins can pause** by setting K = 0 (for maintenance, or a stuck event).

**Live sessions** (a dogfight, a surface drive) run on their own local seconds, but the universe clock keeps going: a 10-minute fight is about 2.5 universe days. The rules:

- The session starts from a **snapshot** of the participants and the local system.
- **Universe events that involve the participants are held** until the session ends: their arrivals, incoming messages, attacks on them. Everyone else's universe carries on normally.
- The session's result is committed as commands at its end UT. Then the held events are processed in order against the new state.

## 2. Motion: trips are equations

A probe's movement is stored as a **Trajectory**: origin, destination, departure UT and proper acceleration. Interstellar trips accelerate to the midpoint, flip, and decelerate. Position and velocity at any UT are computed in closed form, so nothing is ticked, ever.

Relativistic constant-acceleration motion, in units where c = 1, distances in light-years and time in years, with a = 2G ≈ 2.06 ly/yr²:

- Distance covered after coordinate time t from rest: `x(t) = (√(1 + (a·t)²) − 1) / a`
- Time to cover distance x from rest: `t(x) = √((1 + a·x)² − 1) / a`
- Probe's own elapsed time: `τ(x) = acosh(1 + a·x) / a`
- A trip of distance D takes `2·t(D/2)` in the universe frame and `2·τ(D/2)` aboard. Tau Ceti: about 12.8 years universe time, about 3.2 years aboard.

At departure the server computes the arrival UT and schedules a single **Arrival** event. Anyone asking "where is P-7?" gets `position(UT_now)`. The same function handles short in-system hops (the relativistic terms just vanish).

**Boundary rules** (tests must cover each):
- Before departure, the probe is at the origin. After arrival it's exactly at the destination, with zero velocity.
- A zero-distance trip arrives at departure.
- Test both sides of the midpoint.
- Test very short and very long trips.
- The arrival time must be checked in tests against independently computed values. Tau Ceti at 2G is 12.8 universe years and 3.18 aboard; Alpha Centauri is 5.3 and 2.32. Constants: g = 9.80665 m/s², c = 299,792,458 m/s, a Julian year of 365.25 days.

**Precision:** Unity's `Vector3` is 32-bit float and can't tell two points a few kilometers apart at interstellar distances. The core library uses its **own double-precision vector type** (`Vec3d`) and its own units:
- light-years between stars,
- astronomical units inside a system,
- meters locally.

The client converts to `Vector3` only for rendering, relative to the camera ("floating origin").

## 3. Everything else that changes over time: rates and events

Two patterns cover nearly all of the simulation.

- **Lazy accrual (rates).** A mine records `{amount_at, UT_at, rate, cap}` instead of adding ore every second. Current amount = `min(cap, amount_at + rate × (UT_now − UT_at))`. Rules:
  - **Settle before every change:** to the rate, the cap, a spend or a transfer. The amount is brought up to the current UT, then the change is applied.
  - **Resources are integers** in their smallest unit (for example kilograms), never floating point, so the order of events can never create or destroy ore. Settlement adds only whole units and advances `UT_at` by exactly the time those units took, so the fractional remainder carries forward instead of being lost.
  - **Lowering the cap below the current amount** stops accrual but never deletes what's stored.
- **Scheduled events.** Anything that happens at a known future time (arrival, a print job finishing, a foundry completing, a crisis starting, a message hop) goes into the **event scheduler**:
  - It's a database table `(event_id, due_UT, seq, type, payload, status)` plus an in-memory priority queue rebuilt from it on startup.
  - **Ordering is deterministic:** by `due_UT`, then `seq`, a global counter assigned at scheduling. Events at the same UT run in the order they were scheduled, including one scheduled *by* an event at that same UT.
  - **Processing is atomic and duplicate-safe:** one transaction applies the event's effects, schedules any follow-up events, appends to the event log and marks the event done. A crash before the commit means it simply runs again on restart, and a crash after means it's already done. Handlers must therefore be **idempotent**, since running twice must not double anything. That's simple here, because the claim and the effects share one transaction in one database.
  - **Overdue events** (already due at startup) are processed in order before the clock resumes.
  - **Pushing updates to clients happens after the commit**, from the event log, so a client never sees an outcome that was rolled back.

Combined with trajectories, this means a universe of thousands of probes costs almost nothing between events.

## 4. Commands: one door for every change

Every change to the universe is a **Command**: `TravelTo`, `Mine`, `Print`, `BuildFoundry`, `StoreBackup` and so on. A command:

1. arrives from a client, a probe's autonomous behavior, a crisis generator, or a test, with a **command ID** generated by the sender. A command ID the server has already applied returns the original outcome instead of applying twice, so reconnects and retries are always safe;
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
- **State tables** hold the current truth: players, probes, systems, foundries, caches, backups, the scheduler table, the clock checkpoint and applied command IDs.
- **Schema changes go through versioned migration scripts** in the repo. The database is never edited by hand.
- **An event log table** is append-only: every applied command and outcome, with UT and real time. It's used for audit, replay when debugging, and analytics. The log is *not* the source of truth for v1 (it isn't event sourcing). State tables are, which keeps queries and fixes simple.
- **Every command is one transaction.** A crash mid-command leaves nothing half-done. That matters for **foundry backups**: when a seed probe dies, it restores from the last *committed* backup, never a partial one.
- **Nightly database dumps** to disk, later also off-site, and **a restore is tested** regularly by loading a dump into a scratch database and running the server against it. A backup that's never been restored isn't a backup.

## 6. Knowledge and what each client sees

Each player has a **knowledge base**: observations their probes made, each stamped with *when* it was observed. A client is sent:
- its own fleet,
- its knowledge base,
- public things (foundries, Earth broadcasts),
- live updates only for systems where it has a presence.

It never receives the whole universe. This keeps traffic small. It's also the game's fog of war, and it's literally what a **backup restores**: knowledge up to the backup time.

## 7. Communications: messages are physical

In-game communication (probe reports, directives, Earth broadcasts, crisis calls) is **not instant**, and how far and fast it goes is a **progression system** (see "Communication and travel ladder" in the design plan).

- A **Message** is a real object: sender, recipient, payload and send UT. It travels **hop by hop (store-and-forward)**. Each hop is a scheduled event (section 3), and at each node the next hop is chosen from the network *as it is then*. If the next node has vanished, the message reroutes, or waits at the current node until a route exists. Nothing is computed end to end at send time, so a relay destroyed mid-flight is handled naturally.
- The **comm network is one shared, public graph** for the whole universe. Its fixed nodes are Earth, foundries, colonies and **relay stations**, and every player's messages can route through any of them, whoever built them. Probes are endpoints: they send and receive, but don't carry other players' traffic.
  - **Signal links** travel at c. The starting range is **about 5 light-years** (David's decision): Earth's breakthrough reaches the nearest stars, such as Alpha Centauri and Barnard's Star, and everything beyond needs relays. Range grows with equipment tiers. Delay is distance in light-years, in universe years.
  - **Relay stations** are built by players and open to all. The builder is recorded and rewarded (what they earn is still open). Attacking a relay hurts everyone, so it carries a prime directive and standing cost.
  - **Foundries can't be cut off** (David's decision). A foundry can only be started in a system with a relay path to Earth. When it's built, the relays on that path become **anchored**: they can't be attacked or destroyed. So every foundry can always reach Earth, new players starting there are always connected, and backups stored there are always reachable. Unanchored relays can still be lost.
  - **Message contents stay private** to sender and recipient even though the route is public.
  - **Courier links:** with no signal path, a message can ride a probe physically, arriving with the courier's trajectory. Once FTL travel exists, this is the fastest way to carry news.
  - **Advanced links** (wormholes and the like) are just edges with other speeds and ranges.
- At each hop, routing picks the next node on the shortest-delivery-time path from here, with **ties broken by node ID** so the result is deterministic. Unlocking a technology changes only edge types and ranges, so no other system changes. v1 implements signal links and relays only; courier and advanced links are designed for, not built.
- **Directives have the same delay.** An order to a probe takes effect when the message *arrives*. That's why probes need autonomy: out of contact, they act on their last directive. Rogue risk lives in that gap.
- **Knowledge (section 6) arrives by message.** What a player knows about a distant system is as old as the last message that reached them. A backup at a foundry holds whatever had reached that foundry.

## 8. Networking

- **Universe traffic** (commands, outcome events, knowledge updates) goes over **WebSocket** with JSON, which can move to a binary format later if needed. It's simple, works through firewalls, and is well supported on both ends.
- **Live sessions** (later phase) use UDP through Unity's networking stack. A session is a small instance: the universe server hands the participants and starting state to a **session host**, the session plays out, and it reports the result back as commands. Space combat can probably run in plain .NET. Multiplayer surface driving needs Unity physics, so it will likely need a headless Unity session host. That's deferred until Phase 4/6. Until then, surface play is single-player and reports its results.
- **Steam** handles login: the client gets a Steam session ticket, and the server verifies it with Steam. Universe state never lives on Steam.
- **Versions are checked on connect.** The client sends its protocol and rules versions. A client that's too old is told to update rather than allowed to play with mismatched rules.
- **Reconnecting** resumes from the client's last acknowledged event-log position, so nothing is missed or doubled. Commands are retried with the same command ID (section 4).

## 9. Operational basics

These are small, but needed even at 100 players.

- **Authorization:** every command is checked against who sent it. You can only command your own probes, spend your own caches, and so on. That check lives in the command validators, not in the UI.
- **Rate limits** per connection and per player on commands and messages.
- **Logs and metrics** from day one: structured logs, plus counters for commands applied and rejected, event lag (how overdue the oldest due event is) and connected clients. A status page on bigiron, viewable over Tailscale, shows them.
- **Admin tools:** pause the clock, inspect a probe or event, and re-run a stuck event, all as commands with their own audit trail.

## 10. World generation

- **Real stars** come from a public star catalogue (for example HYG; check its license before shipping). Their positions, spectral types and names are fixed.
- **Planets and everything below them** are generated **deterministically from the star's seed**, so any server can regenerate any system identically. Only player-made changes (mines, colonies, foundries) are stored.
- **AI-authored content** (Phase 8) is stored as *overrides* on top of a generated system, and is only ever added to systems that no player has discovered yet, so existing discoveries never change under anyone.
- **Each system's palette** (from the look survey) is part of its generated data.

## 11. Code layout

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

## 12. Growing beyond one server

Not designed yet, on purpose. v1 is one server and one database, with clean module boundaries. Everything is keyed by star system, which is the most likely split line, but global things (players, foundries, the relay network, backups) cross it. A multi-server design should be drawn up only when load says it's needed.

## 13. Build order

From the review, and agreed. Each step proves one claim before the next builds on it:

1. **Headless .NET solution and the command path.** An in-memory universe, one command, deterministic outcomes, and controller-written tests. This also proves the new AI build loop before a database or Unity is involved.
2. **`Vec3d` and trajectory math.** Position, velocity and arrival UT, with explicit units and the boundary tests from section 2.
3. **Lazy accrual.** Mining, settlement, spending and caps, with tests around rate changes and event ordering.
4. **Clock service and event scheduler**, in memory: deterministic ordering, same-UT chains, overdue processing.
5. **Persistence and restart recovery.** PostgreSQL, a transaction per command and per event, duplicate-safe processing, and a kill-and-restart test.
6. **One relay scenario end to end.** Earth → relay → probe with delayed messages, plus a route-loss case and anchoring when a foundry is founded.

Only after step 6 does the Unity client start talking to the server.

**How the work gets built** (the AI build loop, revised Nov 11, 2026):
- The controller writes each task's spec **and its tests**. The tests are checked to compile against an empty stub before any builder sees them.
- Both local models implement the task independently. Whichever passes is kept; if both pass, the controller picks one.
- Feedback is one error at a time, with the exact line and expected vs actual.
- A task still failing after its attempts goes to the controller instead of being blocked.
- Most tasks are plain .NET, tested with `dotnet test`. Unity tests are for client work only.

**New setup needed on bigiron:** the .NET SDK, and Docker access for PostgreSQL (add `dpeters` to the `docker` group). Both need sudo.

## Open questions for David

1. **Relay economics.** What do relay builders earn (standing, a share of traffic credit, naming rights)? And what exactly does attacking an unanchored relay cost the attacker?
2. **Clock factor.** Is 1 real day = 1 universe year the right feel? It's easy to tune later, but the first playtest should pick one.
3. **Are Earth crises shared or per player?** (Already an open question in the plan.) With one shared clock and physical messages, *shared* crises fall out naturally. Players hear about them when Earth's broadcast reaches them through the network, so far-flung or disconnected players hear late or not at all.
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
| 7 | 2026-11-11 | ~~Star systems are the unit of ownership~~ → state keyed by star system; no distributed design for v1 | Review: global players, foundries and relays cross system lines; don't design for many servers without evidence |
| 8 | 2026-11-11 | The controller writes tests; two local models implement independently | The critic role failed in T001 run 1; tests are acceptance criteria |
| 9 | 2026-11-11 | Messages are physical objects routed hop by hop over one shared public comm graph; relays are open to all; a foundry needs a relay path to Earth | David's comms ladder (range-limited light-speed signals → relays → FTL travel → wormholes) is progression; shared relays reward cooperation; foundries are always connected |
| 10 | 2026-11-11 | The universe pauses while the server is down; the clock is a persisted checkpoint that only moves forward | David: fair to players on a single home server; review: the clock needs explicit rules |
| 11 | 2026-11-11 | Relays on a foundry's path to Earth become anchored (indestructible) | David: foundries can't be cut off, so spawns and backups are always reachable |
| 12 | 2026-11-11 | Starting signal range about 5 ly | David: the breakthrough reaches the nearest stars; relays are needed beyond |
| 13 | 2026-11-11 | Scheduled events: deterministic order (due UT, seq); one transaction per event; idempotent handlers; command IDs for retries | Review: crash and retry safety must be explicit |
| 14 | 2026-11-11 | Resources are integers; settle before every change | Review: float accrual can create or lose resources depending on event order |
| 15 | 2026-11-11 | Live sessions start from a snapshot; universe events involving participants are held until the session commits | Review: the compressed clock keeps running during a session |
