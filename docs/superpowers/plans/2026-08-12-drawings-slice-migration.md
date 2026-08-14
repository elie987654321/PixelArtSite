# Drawings Slice Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the drawings feature out of the archived `backend/old/PixelArt.Api` and into the five-project core/external structure, so `/api/drawings` works again and the gallery loads.

**Architecture:** The slice is split the same way auth already is. `Drawing` and `PixelGrid` are pure and go to Core.Domain. `IDrawingRepository` is a port in Core.Abstraction. `DrawingService` and `DrawingPolicy` hold the use cases and the rules in Core.Application, and signal failure with exceptions deriving from `UseCaseException`. EF Core mapping and the repository implementation live in External.Infrastructure. The controller and its DTOs live in External.Interface. Pixel storage does not change: the `string[][]` grid is still serialized to JSON in an `nvarchar(max)` column using the existing converter and comparer.

**Tech Stack:** .NET 9, ASP.NET Core MVC, EF Core 9 (SQL Server), `System.Text.Json`. No new NuGet packages.

**Spec:** [docs/superpowers/specs/2026-08-12-drawings-slice-migration-design.md](../specs/2026-08-12-drawings-slice-migration-design.md)

## Global Constraints

- **🚫 No git writes.** [CLAUDE.md](../../../CLAUDE.md) forbids every agent — including subagents executing this plan — from running any git command that changes anything. Each task ends by handing the exact commit command to the human. **Write the command, do not run it.**
- **No new NuGet dependencies.** Everything needed is already referenced by the target projects.
- **No comments in new code.** Explanations go in the response text, not in the file.
- **TDD, with the test tasks kept out of the way.** Every unit that is a pure function is written test-first. All test code lives in [Appendix A](#appendix-a-test-tasks) at the end of this document; the implementation tasks below carry only a one-line pointer to the test task that precedes them. Read the spine, skip the appendix.
- **Not everything gets a unit test.** `DrawingRepository`, `AppDbContext`, and the controller need a database or an HTTP pipeline; they are covered by the live verification in Task 7 instead.
- **Target framework** `net9.0`; `ImplicitUsings` and `Nullable` are enabled in every project. C# collection expressions (`[]`) are available.
- **Dimension limits:** `MinimumDimension = 1`, `MaximumDimension = 256`. **Name limit:** 1–100 characters. **Colour format:** `#RRGGBBAA`, exactly 8 hex digits, alpha always explicit.
- **One error per validation call.** On a malformed grid, throw on the first problem. A 256×256 grid of bad values must not produce 65,536 messages.
- **Storage is unchanged.** Do not alter the value converter's JSON shape — existing rows must keep deserializing.
- All commands run from the repo root: `c:\dev\Formation\SitePixelArt\PixelArtSite`.
- Build with: `dotnet build backend/src/PixelArt.sln`

## File Structure

**Create:**

| File | Responsibility |
|---|---|
| `backend/src/core/domain/Entities/Drawing.cs` | The drawing entity. Plain data, no dependencies. |
| `backend/src/core/domain/PixelGrid.cs` | Structural compare / hash / deep-copy for a jagged pixel grid. Pure functions. |
| `backend/src/core/abstraction/Persistence/IDrawingRepository.cs` | The persistence port the core calls. |
| `backend/src/core/application/Drawings/Exceptions/DrawingNotFoundException.cs` | Missing or not-yours drawing → 404. |
| `backend/src/core/application/Drawings/Exceptions/InvalidDrawingException.cs` | Any `DrawingPolicy` violation → 400. |
| `backend/src/core/application/Drawings/DrawingPolicy.cs` | The single definition of "is this drawing acceptable". |
| `backend/src/core/application/Drawings/DrawingService.cs` | List / get / create / update / delete use cases. |
| `backend/src/external/infrastructure/Persistence/DrawingRepository.cs` | EF Core implementation of the port. |
| `backend/src/external/interface/Dtos/DrawingRequest.cs` | Inbound wire shape for create and update. |
| `backend/src/external/interface/Dtos/DrawingResponse.cs` | Outbound wire shape. The entity minus `UserId`. |
| `backend/src/external/interface/Controllers/DrawingsController.cs` | The five HTTP endpoints. |

**Modify:**

| File | Change |
|---|---|
| `backend/src/external/infrastructure/Persistence/AppDbContext.cs` | Add the `Drawings` DbSet, the FK to `User`, and the `Pixels` conversion. |
| `backend/src/external/infrastructure/DependencyInjection.cs` | Register `IDrawingRepository`. |
| `backend/src/core/application/DependencyInjection.cs` | Register `DrawingService`. |
| `backend/src/external/interface/ErrorHandling/UseCaseExceptionHandler.cs` | Map `DrawingNotFoundException` to 404. |

**Generated:** a new EF migration under `backend/src/external/infrastructure/Migrations/`.

**Test files** — all defined in [Appendix A](#appendix-a-test-tasks):

| File | Covers |
|---|---|
| `backend/src/tests/PixelArt.Core.Tests/PixelArt.Core.Tests.csproj` | xUnit project, referenced by the solution |
| `backend/src/tests/PixelArt.Core.Tests/PixelGridTests.cs` | compare / hash / deep-copy |
| `backend/src/tests/PixelArt.Core.Tests/DrawingPolicyTests.cs` | one test per validation rule |
| `backend/src/tests/PixelArt.Core.Tests/Drawings/FakeDrawingRepository.cs` | in-memory port double, no database |
| `backend/src/tests/PixelArt.Core.Tests/Drawings/DrawingServiceTests.cs` | use cases against the fake |

## Out of scope (deliberately)

- **Changing pixel storage.** A compact binary or PNG column would be smaller, but it changes the frontend contract and needs a data migration. Separate work.
- **Porting the old test project.** `old/PixelArt.Api.Tests` is left where it is; the new test project is created fresh in Appendix A.
- **Frontend changes.** [drawing.repository.ts](../../../frontend/src/app/repository/drawing.repository.ts) already points at `http://localhost:5126/api/drawings` and the auth interceptor already attaches the bearer token. Nothing needs to change for the gallery to work.
- **The `old/` tree.** Nothing in `backend/old/` is edited or deleted.

---

### Task 1: Domain — entity and pixel grid

**Files:**
- Create: `backend/src/core/domain/Entities/Drawing.cs`
- Create: `backend/src/core/domain/PixelGrid.cs`

**Interfaces:**
- Consumes: nothing. Core.Domain has no project references.
- Produces: `PixelArt.Core.Domain.Entities.Drawing` with properties `Id:int`, `Name:string`, `Width:int`, `Height:int`, `Pixels:string[][]`, `CreatedAt:DateTime`, `UserId:int`. `PixelArt.Core.Domain.PixelGrid` with `AreEqual(string[][]?, string[][]?):bool`, `ComputeHashCode(string[][]):int`, `DeepCopy(string[][]):string[][]`.

> **TDD:** complete [Test T0](#test-t0--test-project) and [Test T1](#test-t1--pixelgrid) before Step 2 of this task. `Drawing` is plain data with no behaviour, so it has no test of its own.

- [ ] **Step 1: Create the entity**

`backend/src/core/domain/Entities/Drawing.cs`

```csharp
namespace PixelArt.Core.Domain.Entities;

public class Drawing
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
}
```

- [ ] **Step 2: Create the pixel grid helper**

Ported verbatim from `backend/old/PixelArt.Api/Domain/PixelGridUtility.cs`, with the namespace changed. Do not alter the logic — EF's change tracking depends on these three functions agreeing with each other.

`backend/src/core/domain/PixelGrid.cs`

```csharp
namespace PixelArt.Core.Domain;

public static class PixelGrid
{
    public static bool AreEqual(string[][]? a, string[][]? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (ReferenceEquals(a, b)) return true;
        if (a.Length != b.Length) return false;

        for (var y = 0; y < a.Length; y++)
        {
            var rowA = a[y];
            var rowB = b[y];
            if (rowA is null && rowB is null) continue;
            if (rowA is null || rowB is null) return false;
            if (rowA.Length != rowB.Length) return false;

            for (var x = 0; x < rowA.Length; x++)
            {
                if (rowA[x] != rowB[x]) return false;
            }
        }

        return true;
    }

    public static int ComputeHashCode(string[][] grid)
    {
        var hash = new HashCode();
        foreach (var row in grid)
        {
            if (row is null) { hash.Add(0); continue; }
            foreach (var pixel in row)
            {
                hash.Add(pixel);
            }
        }

        return hash.ToHashCode();
    }

    public static string[][] DeepCopy(string[][] grid)
    {
        var copy = new string[grid.Length][];
        for (var y = 0; y < grid.Length; y++)
        {
            var row = grid[y];
            copy[y] = row is null ? null! : (string[])row.Clone();
        }

        return copy;
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Hand the commit to the human**

Do not run this. Print it and stop:

`PixelGrid.cs` is committed together with its tests in [Test T1](#test-t1--pixelgrid), so this commit covers the entity only:

```bash
git add backend/src/core/domain/Entities/Drawing.cs
git commit -m "Add Drawing entity to Core.Domain"
```

---

### Task 2: Abstraction — the repository port

**Files:**
- Create: `backend/src/core/abstraction/Persistence/IDrawingRepository.cs`

**Interfaces:**
- Consumes: `PixelArt.Core.Domain.Entities.Drawing` from Task 1.
- Produces: `PixelArt.Core.Abstraction.Persistence.IDrawingRepository` with `ListAsync(int userId, CancellationToken):Task<IReadOnlyList<Drawing>>`, `FindAsync(int id, int userId, CancellationToken):Task<Drawing?>`, `CreateAsync(Drawing, CancellationToken):Task`, `UpdateAsync(Drawing, CancellationToken):Task`, `DeleteAsync(Drawing, CancellationToken):Task`.

Every read takes `userId` so ownership filtering happens in the query, not after it. A caller cannot accidentally load someone else's drawing and forget to check.

- [ ] **Step 1: Create the port**

`backend/src/core/abstraction/Persistence/IDrawingRepository.cs`

```csharp
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Abstraction.Persistence;

public interface IDrawingRepository
{
    Task<IReadOnlyList<Drawing>> ListAsync(int userId, CancellationToken cancellationToken = default);

    Task<Drawing?> FindAsync(int id, int userId, CancellationToken cancellationToken = default);

    Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default);

    Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default);

    Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Hand the commit to the human**

```bash
git add backend/src/core/abstraction/Persistence/IDrawingRepository.cs
git commit -m "Add IDrawingRepository port to Core.Abstraction"
```

---

### Task 3: Application — exceptions and validation

**Files:**
- Create: `backend/src/core/application/Drawings/Exceptions/DrawingNotFoundException.cs`
- Create: `backend/src/core/application/Drawings/Exceptions/InvalidDrawingException.cs`
- Create: `backend/src/core/application/Drawings/DrawingPolicy.cs`

**Interfaces:**
- Consumes: `PixelArt.Core.Application.Exceptions.UseCaseException` (already exists, used by the auth slice).
- Produces: `PixelArt.Core.Application.Drawings.Exceptions.DrawingNotFoundException(int id)`, `InvalidDrawingException(string reason)`, and `PixelArt.Core.Application.Drawings.DrawingPolicy.Validate(string name, int width, int height, string[][] pixels):void`.

Both exceptions derive from `UseCaseException`, so `InvalidDrawingException` reaches 400 through the handler's existing `_` branch with no handler change. `DrawingNotFoundException` needs the explicit case added in Task 6.

> **TDD:** complete [Test T2](#test-t2--drawingpolicy) before Step 3 of this task. The two exception types are declarations with no logic and need no test.

- [ ] **Step 1: Create the not-found exception**

`backend/src/core/application/Drawings/Exceptions/DrawingNotFoundException.cs`

```csharp
using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Drawings.Exceptions;

public sealed class DrawingNotFoundException : UseCaseException
{
    public DrawingNotFoundException(int id) : base($"Drawing {id} was not found.")
    {
    }
}
```

- [ ] **Step 2: Create the validation exception**

`backend/src/core/application/Drawings/Exceptions/InvalidDrawingException.cs`

```csharp
using PixelArt.Core.Application.Exceptions;

namespace PixelArt.Core.Application.Drawings.Exceptions;

public sealed class InvalidDrawingException : UseCaseException
{
    public InvalidDrawingException(string reason) : base(reason)
    {
    }
}
```

- [ ] **Step 3: Create the policy**

`Uri.IsHexDigit` is a BCL static method — no regex, no new package. The colour check runs on up to 65,536 cells, so it must stay allocation-free; do not rewrite it with `Regex` or LINQ.

`backend/src/core/application/Drawings/DrawingPolicy.cs`

```csharp
using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Application.Drawings;

public static class DrawingPolicy
{
    public const int MaximumNameLength = 100;

    public const int MinimumDimension = 1;

    public const int MaximumDimension = 256;

    public static void Validate(string name, int width, int height, string[][] pixels)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidDrawingException("Name is required.");

        if (name.Length > MaximumNameLength)
            throw new InvalidDrawingException($"Name must be at most {MaximumNameLength} characters.");

        if (width < MinimumDimension || width > MaximumDimension)
            throw new InvalidDrawingException($"Width must be between {MinimumDimension} and {MaximumDimension}.");

        if (height < MinimumDimension || height > MaximumDimension)
            throw new InvalidDrawingException($"Height must be between {MinimumDimension} and {MaximumDimension}.");

        if (pixels.Length != height)
            throw new InvalidDrawingException($"The drawing must contain exactly {height} rows.");

        for (var y = 0; y < pixels.Length; y++)
        {
            var row = pixels[y];

            if (row is null || row.Length != width)
                throw new InvalidDrawingException($"Row {y} must contain exactly {width} pixels.");

            for (var x = 0; x < row.Length; x++)
            {
                if (!IsHexColour(row[x]))
                    throw new InvalidDrawingException($"Pixel at row {y}, column {x} is not a #RRGGBBAA colour.");
            }
        }
    }

    private static bool IsHexColour(string? value)
    {
        if (value is null || value.Length != 9 || value[0] != '#')
            return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
                return false;
        }

        return true;
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Commit**

Handled by [Test T2](#test-t2--drawingpolicy), Step 5 — the policy and its tests commit together.

---

### Task 4: Application — the use cases

**Files:**
- Create: `backend/src/core/application/Drawings/DrawingService.cs`
- Modify: `backend/src/core/application/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IDrawingRepository` (Task 2), `DrawingPolicy` and both exceptions (Task 3), `Drawing` (Task 1).
- Produces: `PixelArt.Core.Application.Drawings.DrawingService` with `ListAsync(int userId, CancellationToken):Task<IReadOnlyList<Drawing>>`, `GetAsync(int id, int userId, CancellationToken):Task<Drawing>`, `CreateAsync(string name, int width, int height, string[][] pixels, int userId, CancellationToken):Task<Drawing>`, `UpdateAsync(int id, string name, int width, int height, string[][] pixels, int userId, CancellationToken):Task`, `DeleteAsync(int id, int userId, CancellationToken):Task`.

`GetAsync` returns a non-nullable `Drawing` and throws when missing, so the controller has no null branch. Validation runs *before* the database lookup on update, so a malformed payload costs no round trip.

> **TDD:** complete [Test T3](#test-t3--drawingservice) before Step 1 of this task.

- [ ] **Step 1: Create the service**

`backend/src/core/application/Drawings/DrawingService.cs`

```csharp
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Application.Drawings.Exceptions;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Application.Drawings;

public sealed class DrawingService
{
    private readonly IDrawingRepository _drawings;

    public DrawingService(IDrawingRepository drawings)
    {
        _drawings = drawings;
    }

    public Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        _drawings.ListAsync(userId, cancellationToken);

    public async Task<Drawing> GetAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        await _drawings.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

    public async Task<Drawing> CreateAsync(
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default)
    {
        DrawingPolicy.Validate(name, width, height, pixels);

        var drawing = new Drawing
        {
            Name = name,
            Width = width,
            Height = height,
            Pixels = pixels,
            UserId = userId
        };

        await _drawings.CreateAsync(drawing, cancellationToken);

        return drawing;
    }

    public async Task UpdateAsync(
        int id,
        string name,
        int width,
        int height,
        string[][] pixels,
        int userId,
        CancellationToken cancellationToken = default)
    {
        DrawingPolicy.Validate(name, width, height, pixels);

        var drawing = await _drawings.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

        drawing.Name = name;
        drawing.Width = width;
        drawing.Height = height;
        drawing.Pixels = pixels;

        await _drawings.UpdateAsync(drawing, cancellationToken);
    }

    public async Task DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var drawing = await _drawings.FindAsync(id, userId, cancellationToken)
            ?? throw new DrawingNotFoundException(id);

        await _drawings.DeleteAsync(drawing, cancellationToken);
    }
}
```

- [ ] **Step 2: Register it**

`DrawingService` is `AddScoped` because `IDrawingRepository` resolves to a class holding a scoped `AppDbContext`. Registering it as a singleton would fail at startup with a captive-dependency error.

Modify `backend/src/core/application/DependencyInjection.cs` — add the `using` and the second registration:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PixelArt.Core.Application.Auth;
using PixelArt.Core.Application.Drawings;

namespace PixelArt.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationService>();
        services.AddScoped<DrawingService>();

        return services;
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 4: Commit**

Handled by [Test T3](#test-t3--drawingservice), Step 6 — the service and its tests commit together.

---

### Task 5: Infrastructure — mapping, repository, migration

**Files:**
- Modify: `backend/src/external/infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/external/infrastructure/Persistence/DrawingRepository.cs`
- Modify: `backend/src/external/infrastructure/DependencyInjection.cs`
- Generated: `backend/src/external/infrastructure/Migrations/<timestamp>_AddDrawings.cs`

**Interfaces:**
- Consumes: `IDrawingRepository` (Task 2), `Drawing` and `PixelGrid` (Task 1).
- Produces: `PixelArt.External.Infrastructure.Persistence.DrawingRepository`, and `AppDbContext.Drawings:DbSet<Drawing>`.

- [ ] **Step 1: Map the entity**

The converter and comparer are carried over from `backend/old/PixelArt.Api/Data/AppDbContext.cs` unchanged. The comparer matters: without it EF compares the `string[][]` by reference, so mutating a grid in place would never be detected as a change and saves would silently do nothing.

Replace `backend/src/external/infrastructure/Persistence/AppDbContext.cs` with:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelArt.Core.Domain;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Drawing> Drawings => Set<Drawing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Drawing>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var converter = new ValueConverter<string[][], string>(
            grid => JsonSerializer.Serialize(grid, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<string[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string[]>());

        var comparer = new ValueComparer<string[][]>(
            (a, b) => PixelGrid.AreEqual(a, b),
            grid => PixelGrid.ComputeHashCode(grid),
            grid => PixelGrid.DeepCopy(grid));

        modelBuilder.Entity<Drawing>()
            .Property(d => d.Pixels)
            .HasConversion(converter, comparer);
    }
}
```

- [ ] **Step 2: Create the repository**

`UpdateAsync` only calls `SaveChangesAsync` — the entity handed in is already tracked by the same scoped `AppDbContext` that loaded it, so EF writes the mutated properties without an explicit `Update` call.

`backend/src/external/infrastructure/Persistence/DrawingRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Infrastructure.Persistence;

public sealed class DrawingRepository : IDrawingRepository
{
    private readonly AppDbContext _db;

    public DrawingRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _db.Drawings
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<Drawing?> FindAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        _db.Drawings.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, cancellationToken);

    public async Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _db.Drawings.Add(drawing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _db.Drawings.Remove(drawing);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Register the repository**

Modify `backend/src/external/infrastructure/DependencyInjection.cs` — add one line beside the existing `IUserRepository` registration:

```csharp
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDrawingRepository, DrawingRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
```

- [ ] **Step 4: Build before generating the migration**

The migration tool builds the startup project first; generating from a broken tree produces a confusing error.

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 5: Generate the migration**

Run:

```bash
dotnet ef migrations add AddDrawings \
  --project backend/src/external/infrastructure/PixelArt.External.Infrastructure.csproj \
  --startup-project backend/src/external/interface/PixelArt.External.Interface.csproj
```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 6: Read the generated migration before trusting it**

Open the new `Migrations/<timestamp>_AddDrawings.cs`. Confirm all four:

1. `CreateTable(name: "Drawings", ...)` — this is additive; it must not drop or alter `Users`.
2. `Pixels = table.Column<string>(type: "nvarchar(max)", nullable: false)` — the grid maps to a JSON string column, not to a separate table.
3. A foreign key on `UserId` referencing `Users` with `onDelete: ReferentialAction.Cascade`.
4. `Id = table.Column<int>(...).Annotation("SqlServer:Identity", "1, 1")`.

If anything else appears — a change to the `Users` table, a dropped index — stop and report it rather than continuing.

- [ ] **Step 7: Hand the commit to the human**

```bash
git add backend/src/external/infrastructure
git commit -m "Add drawings persistence and AddDrawings migration"
```

---

### Task 6: Interface — DTOs, controller, 404 mapping

**Files:**
- Create: `backend/src/external/interface/Dtos/DrawingRequest.cs`
- Create: `backend/src/external/interface/Dtos/DrawingResponse.cs`
- Create: `backend/src/external/interface/Controllers/DrawingsController.cs`
- Modify: `backend/src/external/interface/ErrorHandling/UseCaseExceptionHandler.cs`

**Interfaces:**
- Consumes: `DrawingService` (Task 4), `DrawingNotFoundException` (Task 3), `Drawing` (Task 1).
- Produces: HTTP endpoints `GET /api/drawings`, `GET /api/drawings/{id}`, `POST /api/drawings`, `PUT /api/drawings/{id}`, `DELETE /api/drawings/{id}`.

- [ ] **Step 1: Create the request DTO**

`backend/src/external/interface/Dtos/DrawingRequest.cs`

```csharp
namespace PixelArt.External.Interface.Dtos;

public class DrawingRequest
{
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];
}
```

- [ ] **Step 2: Create the response DTO**

`UserId` is deliberately absent — the old API returned the entity and leaked it, and the frontend's `Drawing` interface never declared it.

`backend/src/external/interface/Dtos/DrawingResponse.cs`

```csharp
using PixelArt.Core.Domain.Entities;

namespace PixelArt.External.Interface.Dtos;

public class DrawingResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string[][] Pixels { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public static DrawingResponse From(Drawing drawing) => new()
    {
        Id = drawing.Id,
        Name = drawing.Name,
        Width = drawing.Width,
        Height = drawing.Height,
        Pixels = drawing.Pixels,
        CreatedAt = drawing.CreatedAt
    };
}
```

- [ ] **Step 3: Create the controller**

No `[AllowAnonymous]` anywhere: the fallback policy in `Program.cs` already requires an authenticated user by default. No null checks and no try/catch either — `DrawingService` throws, and `UseCaseExceptionHandler` converts.

`backend/src/external/interface/Controllers/DrawingsController.cs`

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PixelArt.Core.Application.Drawings;
using PixelArt.External.Interface.Dtos;

namespace PixelArt.External.Interface.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrawingsController : ControllerBase
{
    private readonly DrawingService _drawings;

    public DrawingsController(DrawingService drawings)
    {
        _drawings = drawings;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DrawingResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var drawings = await _drawings.ListAsync(CurrentUserId, cancellationToken);

        return Ok(drawings.Select(DrawingResponse.From));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DrawingResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var drawing = await _drawings.GetAsync(id, CurrentUserId, cancellationToken);

        return Ok(DrawingResponse.From(drawing));
    }

    [HttpPost]
    public async Task<ActionResult<DrawingResponse>> Create(
        DrawingRequest input,
        CancellationToken cancellationToken)
    {
        var drawing = await _drawings.CreateAsync(
            input.Name, input.Width, input.Height, input.Pixels, CurrentUserId, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = drawing.Id }, DrawingResponse.From(drawing));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        DrawingRequest input,
        CancellationToken cancellationToken)
    {
        await _drawings.UpdateAsync(
            id, input.Name, input.Width, input.Height, input.Pixels, CurrentUserId, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _drawings.DeleteAsync(id, CurrentUserId, cancellationToken);

        return NoContent();
    }
}
```

- [ ] **Step 4: Map not-found to 404**

`InvalidDrawingException` needs no entry — the `_` branch already gives it 400. Only the not-found case needs adding.

Modify `backend/src/external/interface/ErrorHandling/UseCaseExceptionHandler.cs`. Add the using:

```csharp
using PixelArt.Core.Application.Drawings.Exceptions;
```

and one case to the switch:

```csharp
        var status = useCaseException switch
        {
            UsernameTakenException => StatusCodes.Status409Conflict,
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            DrawingNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
```

- [ ] **Step 5: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 6: Hand the commit to the human**

```bash
git add backend/src/external/interface
git commit -m "Add drawings endpoints to External.Interface"
```

---

### Task 7: Verify against the running stack

**Files:** none — verification only.

- [ ] **Step 1: Rebuild and restart the API container**

Run:

```bash
docker compose up -d --no-deps --build api
```

Expected: `Container pixelart-api  Started`

The `AddDrawings` migration applies on startup, because `Program.cs` calls `db.Database.Migrate()`.

- [ ] **Step 2: Confirm the routes now exist**

Run:

```bash
curl -s http://localhost:5126/openapi/v1.json | tr ',' '\n' | grep -oE '"/api/[^"]*"' | sort -u
```

Expected — two drawings paths, carrying five operations between them, alongside the two auth ones:

```
"/api/Auth/login"
"/api/Auth/register"
"/api/Drawings"
"/api/Drawings/{id}"
```

> **Shell state does not persist between tool calls.** Each command runs in a fresh shell, so a `TOKEN=...` assignment made in one step is gone by the next. Every step below that needs the token must set it in the *same* command — that is why the assignment is repeated rather than done once.

- [ ] **Step 3: Get a token**

Run:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"smoketest","password":"Passw0rd!"}' \
  | sed -E 's/.*"token":"([^"]+)".*/\1/')
echo "${TOKEN:0:20}..."
```

Expected: a token prefix beginning `eyJhbGciOiJIUzI1NiI`. If this returns nothing, the `smoketest` user does not exist in this database — register one first with `POST /api/auth/register`.

- [ ] **Step 4: Create a drawing**

Run:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -w "\n[HTTP %{http_code}]\n" -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"smoke","width":2,"height":2,"pixels":[["#FF0000FF","#00FF00FF"],["#0000FFFF","#000000FF"]]}'
```

Expected: `[HTTP 201]` and a body containing `"id"`, `"name":"smoke"`, the pixel grid, and `"createdAt"` — and **no** `userId` field.

- [ ] **Step 5: List, then fetch by id**

Run:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -w "\n[HTTP %{http_code}]\n" http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN"
curl -s -w "\n[HTTP %{http_code}]\n" http://localhost:5126/api/drawings/1 \
  -H "Authorization: Bearer $TOKEN"
```

Expected: `[HTTP 200]` for both; the list contains the drawing created in Step 4.

- [ ] **Step 6: Check the validation and not-found paths**

Run:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -w "\n[HTTP %{http_code}]\n" -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"bad","width":5,"height":2,"pixels":[["#FF0000FF","#00FF00FF"],["#0000FFFF","#000000FF"]]}'

curl -s -w "\n[HTTP %{http_code}]\n" -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"bad","width":2,"height":2,"pixels":[["red","#00FF00FF"],["#0000FFFF","#000000FF"]]}'

curl -s -w "\n[HTTP %{http_code}]\n" http://localhost:5126/api/drawings/999999 \
  -H "Authorization: Bearer $TOKEN"
```

Expected:

| Request | Status | `title` |
|---|---|---|
| width 5, rows of 2 | 400 | `Row 0 must contain exactly 5 pixels.` |
| cell `"red"` | 400 | `Pixel at row 0, column 0 is not a #RRGGBBAA colour.` |
| id 999999 | 404 | `Drawing 999999 was not found.` |

- [ ] **Step 7: Confirm update and delete**

Run:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -o /dev/null -w "PUT    -> HTTP %{http_code}\n" -X PUT http://localhost:5126/api/drawings/1 \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"renamed","width":2,"height":2,"pixels":[["#FFFFFFFF","#FFFFFFFF"],["#FFFFFFFF","#FFFFFFFF"]]}'
curl -s -o /dev/null -w "DELETE -> HTTP %{http_code}\n" -X DELETE http://localhost:5126/api/drawings/1 \
  -H "Authorization: Bearer $TOKEN"
curl -s -o /dev/null -w "GET    -> HTTP %{http_code}\n" http://localhost:5126/api/drawings/1 \
  -H "Authorization: Bearer $TOKEN"
```

Expected: `204`, `204`, then `404`.

- [ ] **Step 8: Load the gallery**

Open `http://localhost:4200` in a browser, log in, and confirm the gallery renders instead of showing the "couldn't reach the API" error.

- [ ] **Step 9: Report**

Summarise: build status, `dotnet test` results, the four verification tables above with actual values, and anything that differed from expectations. Do not claim success for a step that was not run.

---

# Appendix A: Test tasks

Everything below is test code. The implementation spine above is complete without reading it — each test task is referenced from the task it precedes.

Run tests with: `dotnet test backend/src/PixelArt.sln`

### Test T0 — test project

**Files:**
- Create: `backend/src/tests/PixelArt.Core.Tests/PixelArt.Core.Tests.csproj`

**Interfaces:**
- Produces: an xUnit project referencing all three core projects, in the solution under a `tests` folder.

Package versions match `old/PixelArt.Api.Tests` so the toolchain is already proven on this machine.

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\core\abstraction\PixelArt.Core.Abstraction.csproj" />
    <ProjectReference Include="..\..\core\application\PixelArt.Core.Application.csproj" />
    <ProjectReference Include="..\..\core\domain\PixelArt.Core.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add it to the solution**

```bash
dotnet sln backend/src/PixelArt.sln add backend/src/tests/PixelArt.Core.Tests/PixelArt.Core.Tests.csproj --solution-folder tests
```

Expected: `Project ... added to the solution.`

- [ ] **Step 3: Confirm it runs with zero tests**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: build succeeds; the new project reports no tests. This proves the harness works before any test exists.

- [ ] **Step 4: Hand the commit to the human**

```bash
git add backend/src/tests backend/src/PixelArt.sln
git commit -m "Add PixelArt.Core.Tests project"
```

---

### Test T1 — PixelGrid

Precedes Task 1, Step 2.

**Files:**
- Create: `backend/src/tests/PixelArt.Core.Tests/PixelGridTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using PixelArt.Core.Domain;

namespace PixelArt.Core.Tests;

public class PixelGridTests
{
    private static string[][] SampleGrid() =>
    [
        ["#ff0000ff", "#00ff00ff"],
        ["#0000ffff", "#0a141e28"],
    ];

    [Fact]
    public void AreEqual_BothNull_ReturnsTrue()
    {
        Assert.True(PixelGrid.AreEqual(null, null));
    }

    [Fact]
    public void AreEqual_OneNull_ReturnsFalse()
    {
        Assert.False(PixelGrid.AreEqual(SampleGrid(), null));
        Assert.False(PixelGrid.AreEqual(null, SampleGrid()));
    }

    [Fact]
    public void AreEqual_SameInstance_ReturnsTrue()
    {
        var grid = SampleGrid();
        Assert.True(PixelGrid.AreEqual(grid, grid));
    }

    [Fact]
    public void AreEqual_IdenticalContent_ReturnsTrue()
    {
        Assert.True(PixelGrid.AreEqual(SampleGrid(), SampleGrid()));
    }

    [Fact]
    public void AreEqual_DifferentPixel_ReturnsFalse()
    {
        var other = SampleGrid();
        other[0][0] = "#fe0000ff";
        Assert.False(PixelGrid.AreEqual(SampleGrid(), other));
    }

    [Fact]
    public void AreEqual_DifferentDimensions_ReturnsFalse()
    {
        string[][] small = [["#010203ff"]];
        Assert.False(PixelGrid.AreEqual(SampleGrid(), small));
    }

    [Fact]
    public void DeepCopy_ProducesEqualGrid()
    {
        var original = SampleGrid();
        var copy = PixelGrid.DeepCopy(original);
        Assert.True(PixelGrid.AreEqual(original, copy));
    }

    [Fact]
    public void DeepCopy_MutatingCopy_DoesNotAffectOriginal()
    {
        var original = SampleGrid();
        var copy = PixelGrid.DeepCopy(original);

        copy[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", original[0][0]);
        Assert.False(PixelGrid.AreEqual(original, copy));
    }

    [Fact]
    public void ComputeHashCode_EqualGrids_ProduceSameHash()
    {
        Assert.Equal(
            PixelGrid.ComputeHashCode(SampleGrid()),
            PixelGrid.ComputeHashCode(SampleGrid()));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: compile failure — `The name 'PixelGrid' does not exist in the current context`. That is the correct red state; `PixelGrid.cs` does not exist yet.

- [ ] **Step 3: Implement**

Go do Task 1, Step 2.

- [ ] **Step 4: Run to verify they pass**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: `Passed! - Failed: 0, Passed: 9`

- [ ] **Step 5: Hand the commit to the human**

```bash
git add backend/src/tests/PixelArt.Core.Tests/PixelGridTests.cs backend/src/core/domain/PixelGrid.cs
git commit -m "Add PixelGrid with structural comparison tests"
```

---

### Test T2 — DrawingPolicy

Precedes Task 3, Step 3.

**Files:**
- Create: `backend/src/tests/PixelArt.Core.Tests/DrawingPolicyTests.cs`

- [ ] **Step 1: Write the failing tests**

`ValidGrid` is deliberately 2×2 so a dimension mismatch is easy to construct. `Assert.Throws` returns the exception, so each test also pins the message the client will see.

```csharp
using PixelArt.Core.Application.Drawings;
using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Tests;

public class DrawingPolicyTests
{
    private static string[][] ValidGrid() =>
    [
        ["#FF0000FF", "#00FF00FF"],
        ["#0000FFFF", "#000000FF"],
    ];

    [Fact]
    public void Validate_ValidDrawing_DoesNotThrow()
    {
        Validate("art", 2, 2, ValidGrid());
    }

    [Fact]
    public void Validate_LowercaseHex_IsAccepted()
    {
        string[][] grid = [["#ff0000ff", "#00ff00ff"], ["#0000ffff", "#000000ff"]];
        Validate("art", 2, 2, grid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankName_Throws(string name)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate(name, 2, 2, ValidGrid()));

        Assert.Equal("Name is required.", ex.Message);
    }

    [Fact]
    public void Validate_NameTooLong_Throws()
    {
        var name = new string('a', DrawingPolicy.MaximumNameLength + 1);

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate(name, 2, 2, ValidGrid()));

        Assert.Equal("Name must be at most 100 characters.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    public void Validate_WidthOutOfRange_Throws(int width)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", width, 2, ValidGrid()));

        Assert.Equal("Width must be between 1 and 256.", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    public void Validate_HeightOutOfRange_Throws(int height)
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, height, ValidGrid()));

        Assert.Equal("Height must be between 1 and 256.", ex.Message);
    }

    [Fact]
    public void Validate_RowCountDoesNotMatchHeight_Throws()
    {
        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 3, ValidGrid()));

        Assert.Equal("The drawing must contain exactly 3 rows.", ex.Message);
    }

    [Fact]
    public void Validate_RowWidthDoesNotMatchWidth_Throws()
    {
        string[][] grid = [["#FF0000FF"], ["#0000FFFF", "#000000FF"]];

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 2, grid));

        Assert.Equal("Row 0 must contain exactly 2 pixels.", ex.Message);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FF0000")]
    [InlineData("#GG0000FF")]
    [InlineData("FF0000FF")]
    public void Validate_MalformedColour_Throws(string colour)
    {
        string[][] grid = [[colour, "#00FF00FF"], ["#0000FFFF", "#000000FF"]];

        var ex = Assert.Throws<InvalidDrawingException>(
            () => Validate("art", 2, 2, grid));

        Assert.Equal("Pixel at row 0, column 0 is not a #RRGGBBAA colour.", ex.Message);
    }

    private static void Validate(string name, int width, int height, string[][] pixels) =>
        DrawingPolicy.Validate(name, width, height, pixels);
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: compile failure — `DrawingPolicy` and `InvalidDrawingException` do not exist yet.

- [ ] **Step 3: Implement**

Go do Task 3, Steps 1–3.

- [ ] **Step 4: Run to verify they pass**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: `Failed: 0`, with 9 `PixelGrid` tests plus 15 `DrawingPolicy` cases (the `[Theory]` rows count individually).

- [ ] **Step 5: Hand the commit to the human**

```bash
git add backend/src/tests/PixelArt.Core.Tests/DrawingPolicyTests.cs backend/src/core/application/Drawings
git commit -m "Add DrawingPolicy validation with tests"
```

---

### Test T3 — DrawingService

Precedes Task 4, Step 1.

**Files:**
- Create: `backend/src/tests/PixelArt.Core.Tests/Drawings/FakeDrawingRepository.cs`
- Create: `backend/src/tests/PixelArt.Core.Tests/Drawings/DrawingServiceTests.cs`

The fake is what makes `DrawingService` testable with no database — the whole reason `IDrawingRepository` is a port rather than a concrete class.

- [ ] **Step 1: Write the fake repository**

```csharp
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.Core.Domain.Entities;

namespace PixelArt.Core.Tests.Drawings;

internal sealed class FakeDrawingRepository : IDrawingRepository
{
    private readonly List<Drawing> _drawings = [];
    private int _nextId = 1;

    public IReadOnlyList<Drawing> Stored => _drawings;

    public Task<IReadOnlyList<Drawing>> ListAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Drawing>>(
            _drawings.Where(d => d.UserId == userId).ToList());

    public Task<Drawing?> FindAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_drawings.FirstOrDefault(d => d.Id == id && d.UserId == userId));

    public Task CreateAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        drawing.Id = _nextId++;
        _drawings.Add(drawing);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Drawing drawing, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(Drawing drawing, CancellationToken cancellationToken = default)
    {
        _drawings.Remove(drawing);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Write the failing tests**

```csharp
using PixelArt.Core.Application.Drawings;
using PixelArt.Core.Application.Drawings.Exceptions;

namespace PixelArt.Core.Tests.Drawings;

public class DrawingServiceTests
{
    private const int OwnerId = 1;
    private const int OtherUserId = 2;

    private static string[][] Grid() =>
    [
        ["#FF0000FF", "#00FF00FF"],
        ["#0000FFFF", "#000000FF"],
    ];

    private static (DrawingService Service, FakeDrawingRepository Repository) Build()
    {
        var repository = new FakeDrawingRepository();
        return (new DrawingService(repository), repository);
    }

    [Fact]
    public async Task CreateAsync_StoresDrawingWithOwner()
    {
        var (service, repository) = Build();

        var drawing = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        Assert.Equal(1, drawing.Id);
        Assert.Equal("art", drawing.Name);
        Assert.Equal(OwnerId, drawing.UserId);
        Assert.Single(repository.Stored);
    }

    [Fact]
    public async Task CreateAsync_InvalidInput_ThrowsAndStoresNothing()
    {
        var (service, repository) = Build();

        await Assert.ThrowsAsync<InvalidDrawingException>(
            () => service.CreateAsync("art", 5, 2, Grid(), OwnerId));

        Assert.Empty(repository.Stored);
    }

    [Fact]
    public async Task GetAsync_OwnedDrawing_ReturnsIt()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        var found = await service.GetAsync(created.Id, OwnerId);

        Assert.Equal(created.Id, found.Id);
    }

    [Fact]
    public async Task GetAsync_MissingDrawing_Throws()
    {
        var (service, _) = Build();

        var ex = await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.GetAsync(999, OwnerId));

        Assert.Equal("Drawing 999 was not found.", ex.Message);
    }

    [Fact]
    public async Task GetAsync_AnotherUsersDrawing_ThrowsNotFound()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.GetAsync(created.Id, OtherUserId));
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyCallersDrawings()
    {
        var (service, _) = Build();
        await service.CreateAsync("mine", 2, 2, Grid(), OwnerId);
        await service.CreateAsync("theirs", 2, 2, Grid(), OtherUserId);

        var mine = await service.ListAsync(OwnerId);

        Assert.Single(mine);
        Assert.Equal("mine", mine[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStoredFields()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("before", 2, 2, Grid(), OwnerId);

        string[][] updated = [["#FFFFFFFF", "#FFFFFFFF"], ["#FFFFFFFF", "#FFFFFFFF"]];
        await service.UpdateAsync(created.Id, "after", 2, 2, updated, OwnerId);

        var found = await service.GetAsync(created.Id, OwnerId);
        Assert.Equal("after", found.Name);
        Assert.Equal("#FFFFFFFF", found.Pixels[0][0]);
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersDrawing_ThrowsNotFound()
    {
        var (service, _) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.UpdateAsync(created.Id, "hijacked", 2, 2, Grid(), OtherUserId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesDrawing()
    {
        var (service, repository) = Build();
        var created = await service.CreateAsync("art", 2, 2, Grid(), OwnerId);

        await service.DeleteAsync(created.Id, OwnerId);

        Assert.Empty(repository.Stored);
    }

    [Fact]
    public async Task DeleteAsync_MissingDrawing_Throws()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<DrawingNotFoundException>(
            () => service.DeleteAsync(999, OwnerId));
    }
}
```

- [ ] **Step 3: Run to verify they fail**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: compile failure — `DrawingService` does not exist yet.

- [ ] **Step 4: Implement**

Go do Task 4, Step 1.

- [ ] **Step 5: Run to verify they pass**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: `Failed: 0`, now including 10 `DrawingService` tests.

- [ ] **Step 6: Hand the commit to the human**

```bash
git add backend/src/tests/PixelArt.Core.Tests/Drawings backend/src/core/application/Drawings/DrawingService.cs backend/src/core/application/DependencyInjection.cs
git commit -m "Add DrawingService use cases with tests"
```
