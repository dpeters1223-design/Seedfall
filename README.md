# Seedfall

A persistent, shared MMO where each player is a self-replicating Von Neumann seed probe. Unity (C#), Steam.

This repo also holds the AI build system: an OpenAI **controller** that writes each task's spec and tests, and two local **builders** (Qwen2.5-Coder and DeepSeek-Coder) that implement it independently. The tests decide whether work passes.

## Layout

| Path | What's there |
| --- | --- |
| `docs/design-plan.md` | The game design and build plan |
| `docs/architecture.md` | How it's built: server, simulation, persistence, comms. Read this before writing a task |
| `tasks/queue.json` | The task queue, one entry per task. Edit it by hand |
| `tasks/specs/` | One spec file per task, written by the controller (or by hand for now) |
| `controller/instructions.md` | The controller's standing instructions, spec format and pass rules |
| `orchestrator/` | Python scripts that run the loop (checklist steps 10–11; not written yet) |
| `unity/` | The Unity client project (Unity 6.3 LTS), with the test framework set up |
| `tasks/runs/` | Write-ups of build-loop runs and what was learned |
| `logs/` | Run logs and the status file. Not tracked in git |

## Current phase

Phase 1, the architecture proof of concept. The checklist is at the end of `docs/design-plan.md`, and the build order is in section 13 of `docs/architecture.md`. Run the loop by hand first, automate only the proven parts, and schedule an overnight run last.

## Server

Runs on bigiron (Ryzen 5 9600X, 32 GB RAM, GTX 950 with 2 GB VRAM). The local models run on the CPU through Ollama. Connect with `ssh bigiron` over Tailscale.
