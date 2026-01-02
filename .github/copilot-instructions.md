# Copilot Instructions for Wilnaatahl

## Project Overview

Wilnaatahl visualizes genealogical relationships of Gitxsan huwilp members. It is a cross-platform, web-based tool with a React/TypeScript frontend and a core domain model implemented in F# (compiled to JS via Fable). The project is structured for clear separation between UI, view model, and domain logic.

## Key Architecture

- **Frontend:** React (TypeScript) in `src/react-components/` and `src/main.tsx`.
- **Core Logic:** F# domain and view model in `src/Wilnaatahl.Core/` (see `Model.fs`, `ViewModel.fs`, etc.), compiled to JS via Fable for use in the frontend.
- **Interop:** TypeScript code in `src/generated/Model.ts` and `src/generated/ViewModel/ViewModel.ts` is auto-generated from F# for type-safe interop.
- **State Management:** The React app uses a context (`src/context/viewModelContext.ts`) to bridge between the F# view model and React components.

## Developer Workflows

- **Dev server:** `npm run dev` (hot reload, local development)
- **Build for deploy:** `npm run build`
- **Serve build:** `npx serve dist`
- **Unit tests:** `npm test` (runs both JS and F# tests)
- **Code coverage:**
  - `npm run coverage` (generates coverage XML)
  - `npm run report --coveragefile=<path-to-xml>` (generates HTML report)
- **Format TypeScript:** `npm run format` (Prettier)
- **.NET/F# tools:** Use `dotnet tool restore` and `dotnet restore` to set up, and `dotnet build`/`dotnet test` for F# code.

## Project Conventions

- **F# domain logic** is the source of truth for all business rules and data structures. TypeScript types are generated from F#.
- **F# coding conventions** play to F#'s strength as a functional-first language with powerful type inference. F# code in this project prefers functional to object-oriented style, uses type annotations sparingly, and prefers using the latest language features to outdated syntax.
- **F# unit tests** use the xUnit.net test framework for data-driven tests, and the Swensen.Unquote library for fluent assertions.
- **React components** should not duplicate business logic; always use the F#-generated view model for state and actions.
- **State updates** in the UI are dispatched as messages to the F# view model, which returns new state (see `useViewModel` in context).
- **3D rendering** is handled via `@react-three/fiber` in `TreeScene.tsx`.
- **Styling** is via `src/style.css`.

### F# Unit Test Requirements

- All public members must have direct test coverage, including edge cases and composite scenarios.
- Tests must use direct equality assertion operators (e.g., `x =! y`, `x <! y`, etc.), not pattern matching or mutation.
- Avoid pattern matching on union cases or record fields; prefer direct equality checks.
- Do not use mutation or imperative style; tests should be functional and minimal.
- Use `[<Fact>]` for single-case tests and `[<Theory>]` for parameterized edge cases.
- Update expected values to match actual code output when business logic changes.
- Fix all compilation and test errors before considering the work complete.
- Prefer idiomatic, robust, and maintainable F# style in all test code.

## Integration & Patterns

- **Interop:** Never hand-edit files in `src/generated/`; always regenerate from F#.
- **Testing:** F# tests are in `tests/Wilnaatahl.Core.Tests/`. JS/TS tests (if any) should live alongside components.
- **Build config:** Vite is used for frontend builds (`vite.config.ts`).
- **Licensing:** AGPL-3.0 with a non-commercial restriction (see `LICENSE`).

## Key Files & Directories

- `src/Wilnaatahl.Core/Model.fs` – Domain model
- `src/Wilnaatahl.Core/ViewModel/ViewModel.fs` – View model logic
- `src/generated/Model.ts`, `ViewModel.ts` – F#-to-TS interop
- `src/react-components/` – React UI
- `src/context/viewModelContext.ts` – React context for state
- `tests/Wilnaatahl.Core.Tests/` – F# unit tests
- `README.md` – Setup and workflow details

## Examples

- To add a new domain property, update F# in `Model.fs`, regenerate TS types, and use via the view model in React.
- To add a UI feature, dispatch a message to the F# view model and render the new state in a React component.

---

For more, see the `README.md` and comments in key F# and TS files.
