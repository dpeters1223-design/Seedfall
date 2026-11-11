# Seedfall — Game Design & Build Plan

Nov 11, 2026 · @David Peters

## Overview

Seedfall (working title) is a persistent, shared MMO universe where each player is a robot AI seed probe, a self-replicating Von Neumann probe sent from a troubled Earth. The goal is to find and build homes for humanity while exploring a galaxy that keeps growing.

- Audience: a dedicated, geeky sci-fi crowd.
- Engine: Unity (C#). Distribution: Steam.
- Inspiration: Von Neumann probes and the Bobiverse, kept distinct enough to avoid infringement (no named protagonist; Earth's crisis is a mission brief, not a character arc).
- Psychological hook: world-building under scarcity, grinding resources into bigger, better ships and factories.

## Core game design

The player is the original seed probe; everything else grows from it, and losing it is the central risk.

### The player and the fleet

- You start as a single seed probe with a body (a rig), not just a ship.
- Probes replicate using onboard printers, roamers and miners, building space-bound factories that expand your fleet.
- You control every probe you build by issuing directives (e.g. "survey this system", "secure that asteroid belt").
- Probes act with some autonomy that can help or hinder your goals; there is real risk of a probe going rogue.
- Probes can be sent out to gather information and can be lost; that is a recoverable setback.

### Permadeath and backups

- If your seed probe is destroyed, you are dead and restart from a basic seed.
- Unless you stored a backup at a foundry (Earth's or one you built).
- Restoring brings back your knowledge up to the last backup, plus only the resources you cached at that foundry.
- Cached resources (weapons, metals, materials) can only be used at that one foundry.

### Foundries and launch points

- Foundries are rare, server-wide structures visible to everyone; they build seed ships for new players.
- The first foundry is on Earth. When a player builds one in another system (e.g. Tau Ceti), new players can choose to start there instead.
- Building a foundry takes real-world weeks and earns early builders a serious, lasting reward.
- Foundries also hold backups and caches, which makes them valuable and potential targets.
- Replicating your own fleet is separate from building a foundry that spawns new players.

### Travel and milestones

- Travel starts at about 2G sub-light: set it and forget it, then come back to see where you are.
- FTL is a late-game unlock earned through long-term progression, so there is always a next goal.
- Major milestones (yes or no): found a place Earth can colonize? Built a colony? Helped transport colonists?

### Shared, growing universe

- One persistent universe shared by all players.
- AI developers continually add new systems, so early players can't name every planet and there is always something new to explore.

## Exploration, conflict and consequences

Cooperation should pay better than conflict, and every crisis is a choice with lasting consequences.

### Exploration and science

- Players spend time studying systems for scientific discovery, with striking visuals.
- Players land on worlds, build dirt bikes, drive around, and meet NPC and character aliens, getting into trouble or discovery.
- Colonizing and setting up on planets is part of play, not just moving ships in space.

### Combat

- Three kinds of opponents: other players' probes, Earth-made civilization probes, and alien ships or civilizations.
- Skirmishes happen both in space and on planet surfaces.
- Players are incentivized to cooperate with each other and with alien species when possible.

### Prime directive

- The game tracks a prime directive score; players decide how to act, the score records it.
- A low average score costs you cooperation from Earth.

### Earth and colony crises

- Earth is not a controlling master: once you leave, you make the decisions.
- Earth suffers random large-scale crises (conflicts and other disasters) that create urgent calls for help, such as faster colonization or resources.
- Colonies also face crises, and you can choose whether to help.

### Rewards and penalties

- Helping a colony earns resources, knowledge, standing and prime directive points.
- Ignoring a crisis can cost resources, standing and Earth's cooperation.
- Colonies remember how they were treated.

## Technical architecture

The proof of concept runs on one server, built as separate modules so it can later split across multiple stacks and locations.

| Layer | Role |
| --- | --- |
| Simulation server | Authoritative state of the universe: systems, planets, fleets, foundries. Clients send intentions; the server decides outcomes. |
| Persistence database | Stores the universe, player inventories, backups and foundry caches so nothing is lost on restart. |
| Client (Unity) | Rendering, planet surfaces and dirt bikes, interface. Heavy visuals stay on the player's machine. |
| World generation | Creates new systems and planets over time, including AI-authored content, without disrupting existing ones. |

- Hosting: the proof of concept runs on David's brother's server, reached over VPN.
- Distribution: Steam via Steamworks (store page, achievements, settings sync, player authentication). Universe state stays on our own server.
- Playtest: a small Steam playtest or demo branch with a handful of players once the core loop works.

## AI build system

One commercial controller directs two local builder models, and the Unity test run, not either model, decides whether work passes.

| Agent | Model | Job |
| --- | --- | --- |
| Controller | OpenAI model (David has free tokens) | Holds the plan and task queue, writes each task spec, judges pass or fail from test reports |
| Builder A (implementer) | Qwen2.5-Coder (local) | Writes code or scene changes from the spec |
| Builder B (critic) | DeepSeek-Coder (local) | Writes tests and tries to break Builder A's work |

### The loop

1. Controller picks the next task and writes a spec.
2. Builder A implements it.
3. Builder B reviews it and writes tests.
4. Unity runs the tests (edit-mode for logic, play-mode for scene behavior).
5. Controller accepts the work, or sends it back with the failure log attached.
6. On later tasks the builders swap roles, so each both builds and attacks (the GAN-style part).

### Running it

- Local models run through Ollama or vLLM on the server, each exposed as a local API.
- A Python orchestrator script lives on the server. It reads the task queue, calls the controller and builders, runs Unity tests from the command line, logs every step, and writes a status file David can check over VPN.
- Timeouts on every call so a stalled model can't freeze the queue.
- Goal: autonomous runs of 24 to 30 hours once the loop is proven.

## Roadmap

Nine phases take Seedfall from a proof of the build system to a Steam release.

1. **Architecture proof of concept**: single server, controller plus two builders, Unity test loop.
2. **Core simulation**: star systems, planets, probe movement, mining and printing, probe replication, seed probe and backups.
3. **Foundries and persistence**: backup and cache storage, seed ship spawning, launch point choice, multiplayer sync.
4. **Exploration and surface**: planet surfaces, dirt bike, science study, discovery.
5. **Social and crisis systems**: prime directive score, Earth crises, colony crises, colony memory, standing.
6. **Combat and diplomacy**: probes, Earth ships, alien civilizations, cooperation rewards.
7. **Progression and FTL**: upgrades, weapons, tech unlocks, late-game FTL.
8. **World generation and content pipeline**: continual new systems, AI-authored content.
9. **Multi-server stacks and Steam release**: playtest branch, Steamworks integration, store page.

## First steps: proof of concept checklist

Run the loop by hand first, automate only the proven parts, and schedule an overnight run last.

### Server and environment

- [x] 1\. Get server specs (GPU memory, RAM, storage) and confirm VPN access from the office.
- [x] 2\. Install Ollama or vLLM; pull Qwen2.5-Coder (implementer) and DeepSeek-Coder (critic).
- [x] 3\. Confirm the local model APIs respond over VPN, with timeouts set.
- [x] 4\. Create the Unity project on the server with a clean folder structure and the Unity Test Framework enabled.

### Task queue and controller

- [x] 5\. Write one sample task spec: a probe moves between two points and a mining action updates a resource count.
- [x] 6\. Create the task queue file: one entry per task with ID, one-sentence goal, dependencies, expected output, and pass or fail check (plain text or simple JSON, editable by hand).
- [x] 7\. Write the controller's standing instructions (project lead for Seedfall: read the queue, pick the next task, write a spec, judge pass or fail from test output) and define the spec format and pass or fail rules.

### Manual loops

- [ ] 8\. Run the first loops by hand in an OpenAI chat: paste specs to the builders, copy their output back, run Unity tests yourself, paste results to the controller.
- [ ] 9\. Review the first several loops and refine the spec format, queue format and pass rules based on what broke.

### Automation

- [ ] 10\. Write a thin script for the mechanical steps only: call the builder endpoints, run Unity tests from the command line, log results. No design decisions in code yet.
- [ ] 11\. Extend it into the full orchestrator: calls the OpenAI controller via API, reads and updates the task queue, logs every step, writes a status file viewable over VPN.
- [ ] 12\. Run a short batch of tasks while watching the logs; fix failures before scaling up.
- [ ] 13\. After several clean batches, schedule the first overnight run with a cycle budget and a stop condition.

## Open questions and notes

### Still to decide

- ~~Server GPU memory, which sets realistic model sizes.~~ Answered: GTX 950, 2 GB VRAM, so models run on CPU (32 GB RAM). Start with qwen2.5-coder:7b and deepseek-coder-v2:16b (Lite).
- Cycle count and token budget for overnight runs.
- Whether a foundry's location changes a new player's starting resources or nearby systems.
- Whether Earth's crises are shared server-wide events or generated per player.
- How much a probe's autonomy surprises are helpful versus rogue.

### Licensing and ownership

- Qwen2.5-Coder: Apache 2.0, commercial use allowed; keep the license notice with the model files.
- DeepSeek-Coder: custom DeepSeek License, commercial use allowed but with use restrictions that pass on to anyone you redistribute the model to. Read the restrictions list before shipping.
- Under most commercial terms, including OpenAI's, you generally own generated output. Where the model runs doesn't change ownership.
- Purely AI-generated content may have limited copyright protection, so keep a record of what AI generated versus what David designed or edited.
- Check with an IP lawyer before a commercial release. (General information, not legal advice.)

### Open source (later)

- Open source is an option; revenue could come from the official hosted universe, a paid Steam release, donations or crowdfunding, or cosmetics and expansions.
- Parked until the proof of concept shows the game is worth pursuing.
