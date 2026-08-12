# Backend clean-architecture refactor — design

Date: 2026-08-10
Branch: `pixel-17/refactorisation-backend`

## Goal

Rebuild the backend on a core/external (onion) architecture and migrate the auth
feature onto it, without modifying the previous backend.

## Layout

```
backend/
├── Dockerfile
├── old/                         previous backend, archived as-is
│   ├── PixelArt.sln
│   ├── PixelArt.Api/
│   └── PixelArt.Api.Tests/
└── src/
    ├── PixelArt.sln
    ├── core/
    │   ├── abstraction/     PixelArt.Core.Abstraction.csproj          Microsoft.NET.Sdk
    │   ├── application/     PixelArt.Core.Application.csproj          Microsoft.NET.Sdk
    │   └── domain/          PixelArt.Core.Domain.csproj               Microsoft.NET.Sdk
    └── external/
        ├── infrastructure/  PixelArt.External.Infrastructure.csproj   Microsoft.NET.Sdk
        └── interface/       PixelArt.External.Interface.csproj        Microsoft.NET.Sdk.Web
```

## Layer responsibilities

- **Core.Domain** — entities and business rules. Innermost; depends on nothing.
- **Core.Abstraction** — ports and contracts. Interfaces reference domain entities,
  so it sits just outside Domain.
- **Core.Application** — use cases. Orchestrates the ports; owns no I/O. Added because
  Domain-innermost forbids `Domain → Abstraction`, leaving a domain service unable to
  reach a repository port.
- **External.Infrastructure** — driven adapters implementing the ports (EF Core, BCrypt, JWT).
- **External.Interface** — driving adapter and composition root. Hosts ASP.NET Core and
  wires the implementations into DI.

## Dependency graph

Dependencies point inward only. Verified against the `.csproj` files — no core project
references an external one.

| Project | ProjectReferences |
|---|---|
| `PixelArt.Core.Domain` | — |
| `PixelArt.Core.Abstraction` | Domain |
| `PixelArt.Core.Application` | Abstraction, Domain |
| `PixelArt.External.Infrastructure` | Abstraction, Domain |
| `PixelArt.External.Interface` | Abstraction, Application, Domain, Infrastructure |

```
        ┌────────┐
        │ Domain │  (no dependencies)
        └───▲────┘
            │
     ┌──────┴──────┐
     │ Abstraction │  (ports)
     └──▲───────▲──┘
        │       │
┌───────┴─────┐ │ ┌──────────────┐
│ Application │ └─┤Infrastructure│
└──────▲──────┘   └──────▲───────┘
       └────────┬────────┘
             Interface
```

## Auth slice

Where each piece of the old `Auth/` folder landed. There is no single home — the folder
split across three layers.

| Type | Project | Path |
|---|---|---|
| `User` | Core.Domain | `Entities/User.cs` |
| `ITokenService` | Core.Abstraction | `Auth/ITokenService.cs` |
| `IPasswordHasher` | Core.Abstraction | `Auth/IPasswordHasher.cs` |
| `IUserRepository` | Core.Abstraction | `Persistence/IUserRepository.cs` |
| `AuthenticationService` | Core.Application | `Auth/AuthenticationService.cs` |
| `AuthenticatedUser` | Core.Application | `Auth/AuthenticatedUser.cs` |
| `UseCaseException` | Core.Application | `Exceptions/UseCaseException.cs` |
| `UsernameTakenException` | Core.Application | `Auth/UsernameTakenException.cs` |
| `InvalidCredentialsException` | Core.Application | `Auth/InvalidCredentialsException.cs` |
| `TokenService` | External.Infrastructure | `Auth/TokenService.cs` |
| `JwtSettings` | External.Infrastructure | `Auth/JwtSettings.cs` |
| `BCryptPasswordHasher` | External.Infrastructure | `Auth/BCryptPasswordHasher.cs` |
| `AppDbContext` | External.Infrastructure | `Persistence/AppDbContext.cs` |
| `UserRepository` | External.Infrastructure | `Persistence/UserRepository.cs` |
| `AuthController` | External.Interface | `Controllers/AuthController.cs` |
| `RegisterRequest` / `LoginRequest` / `AuthResponse` | External.Interface | `Dtos/` |
| `BearerSecuritySchemeTransformer` | External.Interface | `OpenApi/` |
| `UseCaseExceptionHandler` | External.Interface | `ErrorHandling/` |

### Failure signalling

`Core.Application` cannot reference ASP.NET Core, so it cannot return `Conflict(...)` /
`Unauthorized(...)` the way the old controller did. Failures leave `AuthenticationService`
as exceptions deriving from `UseCaseException`, and `UseCaseExceptionHandler` maps them:

| Exception | Status |
|---|---|
| `UsernameTakenException` | 409 Conflict |
| `InvalidCredentialsException` | 401 Unauthorized |
| any other `UseCaseException` | 400 Bad Request |
| anything else | falls through to the default handler (500) |

The base class is what makes the fall-through safe: an unmapped business failure still
becomes a client error, and a `SqlException` message never reaches the client.

### Composition

Each layer registers its own services — `AddApplication()` in Core.Application and
`AddInfrastructure(configuration)` in External.Infrastructure — so `Program.cs` names no
concrete implementation.

## Verification

- `dotnet build backend/src/PixelArt.sln` — succeeds, 0 warnings, 0 errors, five DLLs emitted.
- Reference graph checked against the `.csproj` files; no outward or cyclic reference.
- EF migration `20260810171739_InitialAuth` generated: `Users` table with an identity `Id`
  and a unique index on `Username`, matching the old schema.
- Nothing inside `backend/old/` was edited.
- Root `.gitignore` already excludes `[Bb]in/` and `[Oo]bj/`.

## Not yet done

- **Drawings slice.** `Drawing`, `PixelGrid`, `DrawingsController`, and the `string[][]`
  value converter still live only in `old/`. `AppDbContext` currently declares `Users` only.
- **`backend/Dockerfile`** still copies and publishes `PixelArt.Api/PixelArt.Api.csproj`, a
  path that no longer exists. It needs repointing at `src/external/interface`.
- **Tests.** `old/PixelArt.Api.Tests` covers `PixelGridUtility` and has no counterpart in
  the new tree. A test project for the new structure is not yet created.
- **Migration history.** `InitialAuth` starts a fresh history that does not match the
  existing database's `__EFMigrationsHistory`. Running against the current dev database
  will conflict; it expects a fresh database.
