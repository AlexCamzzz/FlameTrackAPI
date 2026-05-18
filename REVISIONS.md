# FlameTrack API Revision History

## Core Versions

### v1.6.0 - Neural Advisor
- Added `AiApiKey` to `UserEntity` for per-user credential management.
- Added `UpdateUserRequestDto` and `UserDto` updates to include `AiApiKey`.
- Implemented `AiService` with logic to call OpenAI Chat Completions (gpt-4o-mini).
- Implemented `AiFunction` with `POST /api/ai/ask`.
- Integrated `AiService` with `TransactionService.GetDashboardSummaryAsync` to provide context for AI prompts.

### v1.5.0 - Pocket Dimensions
- Implementation of Sandbox universe for simulations.
- Added `SandboxService` and `SandboxFunction`.

### v1.4.0 - Debt Management
- Implementation of Debt tracker backend logic.
