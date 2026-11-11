# Seedfall

A persistent, shared MMO where each player is a self-replicating Von Neumann seed probe. Unity (C#), Steam.

This repo also holds the AI build system: an OpenAI **controller** that writes task specs, two local **builders** (Qwen2.5-Coder implements, DeepSeek-Coder critiques and writes tests), and the **Unity Test Framework** as the judge of pass or fail.

## Layout

| Path | What's there |
| --- | --- |
| `docs/design-plan.md` | The game design and build plan |
| `tasks/queue.json` | The task queue, one entry per task. Edit it by hand |
| `tasks/specs/` | One spec file per task, written by the controller (or by hand for now) |
| `controller/instructions.md` | The controller's standing instructions, spec format and pass rules |
| `orchestrator/` | Python scripts that run the loop (checklist steps 10–11; not written yet) |
| `unity/` | The Unity project (checklist step 4; not created yet) |
| `logs/` | Run logs and the status file. Not tracked in git |

## Current phase

Phase 1, the architecture proof of concept. The checklist is at the end of `docs/design-plan.md`. Run the loop by hand first, automate only the proven parts, and schedule an overnight run last.

## Server

Runs on bigiron (Ryzen 5 9600X, 32 GB RAM, GTX 950 with 2 GB VRAM). The local models run on the CPU through Ollama. Connect with `ssh bigiron` over Tailscale.
