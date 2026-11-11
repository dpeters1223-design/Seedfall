# Orchestrator

Not written yet. Per the plan, do the first loops by hand (checklist steps 8–9). Then:

- **Step 10, the thin script**: call the builder endpoints (Ollama at `http://localhost:11434`), run Unity tests from the command line, and log results. No design decisions in code.
- **Step 11, the full orchestrator**: call the OpenAI controller via its API, read and update `tasks/queue.json`, log every step to `logs/`, and write `logs/status.json` so it can be checked over VPN.

Every model call gets a timeout so a stalled model can't freeze the queue.

## Model settings on bigiron (benchmarked 2026-11-11)

**Always send `"num_gpu": 0`** in Ollama request options. The GTX 950 (2 GB) gets a sliver of the model by default and slows down prompt reading 5–11×.

Benchmark: implementing the T001 spec, CPU-only, `num_ctx` 8192:

| Model | Reads prompt | Writes code | T001 wall time |
| --- | --- | --- | --- |
| qwen2.5-coder:7b | ~136 tok/s | ~11 tok/s | 38 s |
| deepseek-coder-v2:16b (MoE) | ~195 tok/s | ~27 tok/s | 22 s |

Both produced a correct `Probe.cs` on the first try.
