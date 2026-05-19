# FlameTrack API Revision History

## Core Versions

### v1.6.0 - Neural Advisor
- Added `AiApiKey` and `AiProvider` to `UserEntity` for per-user credential management.
- Refactored `AiService` to support OpenAI (ChatGPT), Google (Gemini), and Anthropic (Claude).
- Implemented specific API protocols and prompt formats for each provider.
- Integrated `AiService` with `TransactionService` for dashboard context.
- Implemented robust error extraction and logging for third-party AI failures.

### v1.5.0 - Pocket Dimensions
- Implementation of Sandbox universe for simulations.
- Added `SandboxService` and `SandboxFunction`.

### v1.4.0 - Debt Management
- Implementation of Debt tracker backend logic.
