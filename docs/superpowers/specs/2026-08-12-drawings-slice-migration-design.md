# Drawings slice migration — design

Date: 2026-08-12
Branch: `pixel-17/refactorisation-backend`
Follows: [2026-08-10-backend-clean-architecture-scaffold-design.md](2026-08-10-backend-clean-architecture-scaffold-design.md)

## Goal

Migrate the drawings feature from `backend/old/PixelArt.Api` into the five-project
core/external structure, matching the shape already established by the auth slice.

## Scope

**In scope:** structural migration of `Drawing`, `PixelGrid`, and `DrawingsController`
across the layers; validation of drawing input; the EF migration that adds the `Drawings`
table; a test project for the pure-logic units.

**Out of scope:** changing how pixels are stored. The `string[][]` grid continues to be
serialized to JSON in an `nvarchar(max)` column with the existing value converter and
comparer. The frontend contract is unchanged.

## Layer placement

| Type | Project | Path |
|---|---|---|
| `Drawing` | Core.Domain | `Entities/Drawing.cs` |
| `PixelGrid` | Core.Domain | `PixelGrid.cs` |
| `IDrawingRepository` | Core.Abstraction | `Persistence/IDrawingRepository.cs` |
| `DrawingService` | Core.Application | `Drawings/DrawingService.cs` |
| `DrawingPolicy` | Core.Application | `Drawings/DrawingPolicy.cs` |
| `DrawingNotFoundException`, `InvalidDrawingException` | Core.Application | `Drawings/Exceptions/` |
| `Drawings` DbSet, converter, comparer | External.Infrastructure | `Persistence/AppDbContext.cs` |
| `DrawingRepository` | External.Infrastructure | `Persistence/DrawingRepository.cs` |
| `DrawingsController` | External.Interface | `Controllers/DrawingsController.cs` |
| `DrawingRequest`, `DrawingResponse` | External.Interface | `Dtos/` |

`PixelGrid` belongs to Core.Domain: it is pure logic over the domain's own pixel
representation with no dependencies. EF's value comparer consumes it, but consuming a type
does not make the consumer its owner. JSON serialization stays in Infrastructure, because
turning a grid into a database column is a storage concern.

## Ownership

Every `DrawingService` method takes a `userId`, and the repository filters on it. A drawing
belonging to another user returns **404, not 403** — preserving the existing behaviour and
not revealing that an id exists.

The controller reads the caller's id from the JWT `sub` claim via
`User.FindFirstValue(ClaimTypes.NameIdentifier)`, unchanged from the old controller.

## Validation

`DrawingPolicy.Validate` runs before create and update, mirroring `PasswordPolicy`:

| Rule | Failure message |
|---|---|
| `Name` not blank | `Name is required.` |
| `Name` ≤ 100 characters | `Name must be at most 100 characters.` |
| `Width` in 1..256 | `Width must be between 1 and 256.` |
| `Height` in 1..256 | `Height must be between 1 and 256.` |
| `Pixels.Length == Height` | `The drawing must contain exactly {height} rows.` |
| each row `Length == Width` | `Row {y} must contain exactly {width} pixels.` |
| each cell matches `#RRGGBBAA` | `Pixel at row {y}, column {x} is not a #RRGGBBAA colour.` |

The colour check runs on up to 65,536 cells, so it is a hand-rolled scan (length 9, leading
`#`, remaining characters hex) rather than a regex evaluated per cell.

All failures throw `InvalidDrawingException`, which derives from `UseCaseException`.

## Error mapping

| Exception | Status | Handler change needed |
|---|---|---|
| `InvalidDrawingException` | 400 | none — falls through to the `_` branch |
| `DrawingNotFoundException` | 404 | one new case in `UseCaseExceptionHandler` |

## Persistence

`AppDbContext` gains a `Drawings` DbSet, the cascade-delete foreign key to `User`, and the
`Pixels` conversion carried over unchanged:

- converter: `JsonSerializer.Serialize` / `Deserialize` to and from `string`
- comparer: `PixelGrid.AreEqual` / `ComputeHashCode` / `DeepCopy`

A new additive EF migration, `AddDrawings`, follows `InitialAuth`. The `PixelArtV2`
database already has `InitialAuth` applied, so no data migration is required.

## API contract

Five endpoints, identical routes and verbs to the old backend:

| Verb | Route | Success |
|---|---|---|
| GET | `/api/drawings` | 200, array of `DrawingResponse` |
| GET | `/api/drawings/{id}` | 200, `DrawingResponse` |
| POST | `/api/drawings` | 201 + `Location`, `DrawingResponse` |
| PUT | `/api/drawings/{id}` | 204 |
| DELETE | `/api/drawings/{id}` | 204 |

Requests use `DrawingRequest` (`name`, `width`, `height`, `pixels`). Responses use
`DrawingResponse` (`id`, `name`, `width`, `height`, `pixels`, `createdAt`) — the entity
minus `UserId`.

Dropping `UserId` is safe: the frontend's `Drawing` interface in
`frontend/src/app/model/drawing.model.ts` declares only the six fields above, so it never
read the field the old API leaked.

## Verification

Development is test-first for every unit that is a pure function. A new
`src/tests/PixelArt.Core.Tests` xUnit project covers:

- `PixelGrid` — compare, hash, and deep-copy behaviour
- `DrawingPolicy` — one case per validation rule, asserting the exact message
- `DrawingService` — the use cases against an in-memory `IDrawingRepository` fake

`DrawingRepository`, `AppDbContext`, and the controller need a database or an HTTP
pipeline, so they are verified by exercising the five endpoints against the running
container with a real bearer token rather than by unit tests.

In the implementation plan the test tasks are collected in an appendix, so the
implementation spine can be read without them.

## Constraint: no git writes by agents

[CLAUDE.md](../../../CLAUDE.md) forbids every agent, including subagents executing this
plan, from running any git command that writes. Tasks therefore end by handing the commit
to the human with the exact command text rather than running it.
