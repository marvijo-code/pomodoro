# Global Codex Rules

- When a workflow becomes recurring, tool-specific, or reusable across tasks, create or update a skill in `C:\dev\AgentSkills`, validate it, and refresh the installed copy under `C:\Users\marvi\.codex\skills`.
- Keep `agents/openai.yaml` metadata aligned with each maintained skill.
- Put short durable expectations in `C:\Users\marvi\.codex\AGENTS.md`; keep the detailed procedure inside the skill itself.
- Prefer updating an existing skill over creating a duplicate when the workflow is substantively the same.
- Prefer using the GitHub CLI (`gh`) for GitHub-related issues when the CLI can handle the task.
- For NanoChat or LLM-training workflows, keep training in a measured loop until there is significant target-metric progress, and update the matching global skill when the workflow changes.
- For autoresearch-style NanoChat work, follow the upstream pattern: GPU-first bounded experiments, paper- or repo-driven hypothesis generation, immediate target-metric evaluation, and keep/discard decisions based on real task improvement rather than loss alone.
- For autoresearch-style LLM work, treat arXiv papers, official repo docs, and primary implementation notes as part of the loop; update the relevant global skill when new research or workflow constraints change how experiments should be run.
