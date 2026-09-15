# Project agent instructions

## Engineering standards

This project adopts [Engineering](https://github.com/noahborkowski/engineering) at revision `28c14b8484ff76e2207a8a3c3ac5a42fe57a068a`, introduced on its default branch by the [merged adoption-guide PR](https://github.com/noahborkowski/engineering/pull/13).

Before work, read the [standards index](https://github.com/noahborkowski/engineering/blob/28c14b8484ff76e2207a8a3c3ac5a42fe57a068a/STANDARDS.md) and [adoption guide](https://github.com/noahborkowski/engineering/blob/28c14b8484ff76e2207a8a3c3ac5a42fe57a068a/playbooks/project-adoption.md). Read the indexed baseline and apply all relevant shared guidance within the task's scope. Keep the recorded revision unless a standards update is explicitly requested.

Engineering is private. Use an authenticated GitHub connection with access to that repository to retrieve these files and their references at the recorded revision. Do not assume a sibling checkout exists. If access is unavailable, report that limitation before work that depends on the shared guidance; do not claim it was loaded.

This repository is public: reference shared material without copying private standards, templates, research, credentials, or local paths into files, issues, or PRs. Keep any project-specific instructions and review requirements intact. Leave PRs unmerged for user review.

## Project context and checks

- Read [README.md](README.md) for project context and existing contribution guidance.
- [LOLBinOrchestrator.csproj](LOLBinOrchestrator.csproj) targets .NET 6. The README documents `dotnet build`; no automated test project is currently tracked.
- For documentation-only changes, check links and the diff; do not run the application or security techniques as a documentation check.
