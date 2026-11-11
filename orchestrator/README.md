# Orchestrator

Not written yet. Per the plan, do the first loops by hand (checklist steps 8–9). Then:

- **Step 10, the thin script**: call the builder endpoints (Ollama at `http://localhost:11434`), run Unity tests from the command line, and log results. No design decisions in code.
- **Step 11, the full orchestrator**: call the OpenAI controller via its API, read and update `tasks/queue.json`, log every step to `logs/`, and write `logs/status.json` so it can be checked over VPN.

Every model call gets a timeout so a stalled model can't freeze the queue.
