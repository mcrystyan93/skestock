Analyze this repository in depth and generate a single, comprehensive instructions file 
(`.github/copilot-instructions.md`) targeted at AI coding agents working in this codebase. 
Cross-reference and stay consistent with the existing `AGENTS.md` and any files under 
`.github/instructions/` and `.github/agents/` — don't contradict them, extend/complement them.

Investigate before writing:
1. Read `AGENTS.md`, `.github/instructions/*`, `.github/agents/*`, `.github/skills/*`, 
   `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `aspire.config.json`, 
   and the `.slnx` solution file.
2. Walk each project under `src/` (AppHost, Application, Domain, Infrastructure, 
   ServiceDefaults, Shared, Web) and each project under `tests/` — note responsibilities, 
   dependency direction, and any non-obvious conventions (naming, DI registration patterns, 
   folder layout per feature/slice).
3. Identify the actual tech stack and versions in use (.NET version, EF Core, MediatR, 
   FluentValidation, Identity, Aspire resources like SQL Server/Redis, testing frameworks).
4. Note build/run/test commands that actually work in this repo (not generic dotnet 
   boilerplate) — including anything requiring Docker, Aspire dashboard, or database reset 
   helpers for tests.
5. Capture cross-cutting conventions: central package management, global usings, guard 
   clauses, MediatR pipeline behaviour order, endpoint registration pattern (IEndpointGroup), 
   auth setup, and any code generation templates (e.g. `dotnet new ca-usecase`).
6. If a frontend (e.g. Angular) exists or is planned per `.github/instructions/`, document 
   its structure, state management (e.g. NgRx SignalStore), UI library conventions (ng-zorro), 
   and how it integrates with the backend API.

Output requirements:
- Structure the file with clear headings: Overview, Architecture & Layer Responsibilities, 
  Solution/Project Layout, Critical Workflows (build/run/test), Conventions & Patterns 
  (DI, MediatR pipeline, endpoints, validation, global usings), Scaffolding/Codegen tools, 
  Auth, Testing strategy, and Frontend (if applicable).
- Be specific and concrete: reference actual file paths, class names, and commands found in 
  the repo — not generic advice.
- Keep it concise but complete enough that an agent unfamiliar with the repo can be 
  productive immediately (target similar depth/density to the existing AGENTS.md).
- Call out anything that deviates from typical Clean Architecture template conventions, 
  since this is based on the Jason Taylor Clean Architecture template but has custom 
  additions (Aspire, Shared project, custom endpoint pattern, etc.).
- Do not duplicate AGENTS.md content verbatim — instead point to it where appropriate and 
  add details AGENTS.md doesn't cover (e.g. deeper file-by-file structure, testing nuances, 
  frontend specifics).