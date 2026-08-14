# PixelGrid Value Object Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `PixelGrid` from a static helper over `string[][]` into an immutable value object with real equality, so the grid owns its own comparison instead of exposing three loose functions for EF to wire together.

**Architecture:** `PixelGrid` becomes a sealed class wrapping a jagged array, copying on the way in and on the way out so no caller can mutate it after construction. `Equals` and `GetHashCode` replace `AreEqual` and `ComputeHashCode`. `DeepCopy` disappears entirely: EF's snapshot function can return the same reference, because an immutable value cannot change underneath the change tracker. `Drawing.Pixels` changes type; nothing else about the entity moves.

**Tech Stack:** .NET 9, EF Core 9, `System.Text.Json`. No new packages.

**Spec:** none — this is a follow-up refinement to [2026-08-12-drawings-slice-migration-design.md](../specs/2026-08-12-drawings-slice-migration-design.md), whose "Out of scope" note deferred exactly this change.

## Global Constraints

- **🚫 No git writes.** [CLAUDE.md](../../../CLAUDE.md) forbids every agent from running any git command that changes anything. Tasks end by handing the exact commit command to the human. **Write the command, do not run it.**
- **No comments in new code.**
- **Storage must not change.** The `Pixels` column stays `nvarchar(max)` holding `[["#RRGGBBAA",...],...]`. Existing rows must keep deserializing. Task 5 proves this by showing EF detects no schema change.
- **The HTTP contract must not change.** `DrawingRequest.Pixels` and `DrawingResponse.Pixels` stay `string[][]`. The frontend is not touched.
- **No behaviour change.** Every status code and every validation message stays exactly as it is today. `DrawingPolicy` is **not modified** — see the decision below.
- **TDD.** Test code lives in [Appendix A](#appendix-a-test-tasks); the spine carries one-line pointers. Read the spine, skip the appendix.
- Build with `dotnet build backend/src/PixelArt.sln`, test with `dotnet test backend/src/PixelArt.sln`, from the repo root `c:\dev\Formation\SitePixelArt\PixelArtSite`.

## Decision: validation stays on the raw array

An immutable grid knows its own `Width` and `Height`, which tempts you to derive `Drawing.Width` / `Drawing.Height` from it and delete two of `DrawingPolicy`'s rules.

**This plan does not do that.** Deriving the dimensions means a request sending `width: 5` with 2-wide rows would be silently corrected to `width: 2` instead of returning `400 Row 0 must contain exactly 5 pixels.` Silently rewriting contradictory client input is worse than rejecting it.

So the order in `DrawingService` is: **validate the raw `string[][]`, then construct the `PixelGrid` from the validated array.** `DrawingPolicy` keeps its exact signature, its seven rules, and its fifteen tests — all untouched.

That also keeps this change small enough to review on its own, which is the point.

## File Structure

**Modify:**

| File | Change |
|---|---|
| `backend/src/core/domain/PixelGrid.cs` | Static helper → immutable value object. |
| `backend/src/core/domain/Entities/Drawing.cs` | `Pixels` becomes `PixelGrid` instead of `string[][]`. |
| `backend/src/external/infrastructure/Persistence/AppDbContext.cs` | Converter and comparer retargeted at `PixelGrid`. |
| `backend/src/core/application/Drawings/DrawingService.cs` | Construct a `PixelGrid` after validation, in two places. |
| `backend/src/external/interface/Dtos/DrawingResponse.cs` | `.ToArray()` when mapping out. |

**Unchanged, deliberately:** `DrawingPolicy.cs`, `DrawingPolicyTests.cs`, `DrawingRepository.cs`, `DrawingsController.cs`, `DrawingRequest.cs`, `IDrawingRepository.cs`, `UseCaseExceptionHandler.cs`, and every migration already generated.

## Out of scope

- **Deriving `Width`/`Height` from the grid.** See the decision above.
- **Changing the storage format.** A packed binary or PNG column is a separate piece of work with a data migration.
- **Making `Drawing` itself immutable.** It is an EF-tracked entity; that is a much larger change.

---

### Task 1: PixelGrid becomes a value object

**Files:**
- Modify: `backend/src/core/domain/PixelGrid.cs`

**Interfaces:**
- Produces: `PixelArt.Core.Domain.PixelGrid` — constructor `PixelGrid(string[][] rows)`; static `Empty:PixelGrid`; `Height:int`; `Width:int`; indexer `this[int y, int x]:string`; `ToArray():string[][]`; `Equals(PixelGrid?):bool`; `Equals(object?):bool`; `GetHashCode():int`.
- Removes: the statics `AreEqual`, `ComputeHashCode`, `DeepCopy`.

> **TDD:** complete [Test T1](#test-t1--pixelgrid-value-object) before Step 1 of this task.

The private `Copy` runs in the constructor *and* in `ToArray`. Both are required: without the first, whoever passed the array in can still mutate the grid; without the second, whoever receives the array can. Those two copies are what let the EF snapshot in Task 3 be a no-op.

`Width` reads row 0 rather than assuming rectangularity, because `DrawingPolicy` — not this type — is what guarantees every row is the same length.

- [ ] **Step 1: Replace the file**

`backend/src/core/domain/PixelGrid.cs`

```csharp
namespace PixelArt.Core.Domain;

public sealed class PixelGrid : IEquatable<PixelGrid>
{
    private readonly string[][] _rows;

    public PixelGrid(string[][] rows)
    {
        _rows = Copy(rows);
    }

    public static PixelGrid Empty { get; } = new([]);

    public int Height => _rows.Length;

    public int Width => _rows.Length == 0 ? 0 : _rows[0]?.Length ?? 0;

    public string this[int y, int x] => _rows[y][x];

    public string[][] ToArray() => Copy(_rows);

    public bool Equals(PixelGrid? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_rows.Length != other._rows.Length) return false;

        for (var y = 0; y < _rows.Length; y++)
        {
            var myRow = _rows[y];
            var theirRow = other._rows[y];

            if (myRow is null && theirRow is null) continue;
            if (myRow is null || theirRow is null) return false;
            if (myRow.Length != theirRow.Length) return false;

            for (var x = 0; x < myRow.Length; x++)
            {
                if (myRow[x] != theirRow[x]) return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as PixelGrid);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var row in _rows)
        {
            if (row is null) { hash.Add(0); continue; }

            foreach (var pixel in row)
            {
                hash.Add(pixel);
            }
        }

        return hash.ToHashCode();
    }

    private static string[][] Copy(string[][] rows)
    {
        var copy = new string[rows.Length][];

        for (var y = 0; y < rows.Length; y++)
        {
            var row = rows[y];
            copy[y] = row is null ? null! : (string[])row.Clone();
        }

        return copy;
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: **FAILS.** `Drawing.Pixels`, `AppDbContext`, `DrawingService`, and `DrawingResponse` all still expect `string[][]`. Tasks 2 to 4 fix them. This is the one point in the plan where a red build is correct — do not try to fix it here.

---

### Task 2: Drawing holds a PixelGrid

**Files:**
- Modify: `backend/src/core/domain/Entities/Drawing.cs`

**Interfaces:**
- Consumes: `PixelGrid` from Task 1.
- Produces: `Drawing.Pixels:PixelGrid`, defaulting to `PixelGrid.Empty`.

`Width` and `Height` stay as their own properties. They remain client-supplied and validated, per the decision above.

- [ ] **Step 1: Change the property type**

`backend/src/core/domain/Entities/Drawing.cs`

```csharp
namespace PixelArt.Core.Domain.Entities;

public class Drawing
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public PixelGrid Pixels { get; set; } = PixelGrid.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
}
```

Note there is no `using` for `PixelGrid` — it lives in `PixelArt.Core.Domain`, the parent of this file's `PixelArt.Core.Domain.Entities` namespace, so it resolves without one.

- [ ] **Step 2: Do not build yet**

The solution stays red until Task 4. Move on.

---

### Task 3: EF maps the value object

**Files:**
- Modify: `backend/src/external/infrastructure/Persistence/AppDbContext.cs`

**Interfaces:**
- Consumes: `PixelGrid` (Task 1), `Drawing.Pixels:PixelGrid` (Task 2).

This is the task that must not be got wrong. **The converter serializes `grid.ToArray()`, never `grid` itself.** Serializing the object would write `{"Height":2,"Width":2}` into the column and every existing row would fail to load.

The snapshot argument becomes `g => g` — no copy. That is only safe because Task 1 made the type immutable; with the old mutable array it would have broken change tracking entirely.

- [ ] **Step 1: Retarget converter and comparer**

Replace the two declarations inside `OnModelCreating`:

```csharp
        var converter = new ValueConverter<PixelGrid, string>(
            grid => JsonSerializer.Serialize(grid.ToArray(), (JsonSerializerOptions?)null),
            json => new PixelGrid(JsonSerializer.Deserialize<string[][]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string[]>()));

        var comparer = new ValueComparer<PixelGrid>(
            (a, b) => a!.Equals(b),
            grid => grid.GetHashCode(),
            grid => grid);
```

Everything else in the file — the `Users` index, the `Drawings` foreign key, the `HasConversion` call — is unchanged.

Note `Array.Empty<string[]>()` rather than the collection expression `[]`. Both converter lambdas compile to **expression trees**, and `CS9175: An expression tree may not contain a collection expression` rejects `[]` there. The `[]` in `PixelGrid.Empty` is fine — that one is ordinary code, not an expression tree.

- [ ] **Step 2: Do not build yet**

Still red until Task 4.

---

### Task 4: Application and Interface mapping

**Files:**
- Modify: `backend/src/core/application/Drawings/DrawingService.cs`
- Modify: `backend/src/external/interface/Dtos/DrawingResponse.cs`

**Interfaces:**
- Consumes: `PixelGrid` (Task 1), `Drawing.Pixels:PixelGrid` (Task 2).
- Produces: no signature changes. `DrawingService` still accepts `string[][] pixels`; `DrawingResponse.Pixels` is still `string[][]`.

> **TDD:** complete [Test T2](#test-t2--drawingservice-assertion) before Step 1 of this task.

Note the order in both methods: `DrawingPolicy.Validate` runs on the raw array **first**, then the validated array is wrapped. That is what keeps `DrawingPolicy` untouched.

- [ ] **Step 1: Wrap on create**

In `CreateAsync`, the entity initialiser becomes:

```csharp
        DrawingPolicy.Validate(name, width, height, pixels);

        var drawing = new Drawing
        {
            Name = name,
            Width = width,
            Height = height,
            Pixels = new PixelGrid(pixels),
            UserId = userId
        };
```

Add the using at the top of the file:

```csharp
using PixelArt.Core.Domain;
```

- [ ] **Step 2: Wrap on update**

In `UpdateAsync`, the assignment becomes:

```csharp
        drawing.Name = name;
        drawing.Width = width;
        drawing.Height = height;
        drawing.Pixels = new PixelGrid(pixels);
```

- [ ] **Step 3: Unwrap on the way out**

In `backend/src/external/interface/Dtos/DrawingResponse.cs`, one line of `From` changes:

```csharp
        Pixels = drawing.Pixels.ToArray(),
```

The property type stays `string[][]`, so the JSON the client receives is identical.

- [ ] **Step 4: Build**

Run: `dotnet build backend/src/PixelArt.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` — the solution goes green again here.

- [ ] **Step 5: Run the tests**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: `Failed: 0`, **37 total**. `DrawingPolicyTests` keeps its 15 and `DrawingServiceTests` its 10; `PixelGridTests` goes from 9 to 12, because [Test T1](#test-t1--pixelgrid-value-object) adds three cases for guarantees the old static helper could not make.

- [ ] **Step 6: Hand the commit to the human**

```bash
git add backend/src/core/domain backend/src/core/application/Drawings/DrawingService.cs backend/src/external/infrastructure/Persistence/AppDbContext.cs backend/src/external/interface/Dtos/DrawingResponse.cs backend/src/tests
git commit -m "Make PixelGrid an immutable value object"
```

---

### Task 5: Prove storage and behaviour are unchanged

**Files:** none — verification only.

- [ ] **Step 1: Confirm EF sees no schema change**

This is the proof that the column is untouched. Generate a throwaway migration and inspect it:

```bash
dotnet ef migrations add VerifyNoSchemaChange \
  --project backend/src/external/infrastructure/PixelArt.External.Infrastructure.csproj \
  --startup-project backend/src/external/interface/PixelArt.External.Interface.csproj
```

Open the generated `Migrations/<timestamp>_VerifyNoSchemaChange.cs`. Both `Up` and `Down` must have **empty bodies**.

If either contains an `AlterColumn` or `DropColumn`, **stop** — the converter is wrong, most likely serializing the wrapper instead of `grid.ToArray()`.

- [ ] **Step 2: Remove the throwaway migration**

```bash
dotnet ef migrations remove \
  --project backend/src/external/infrastructure/PixelArt.External.Infrastructure.csproj \
  --startup-project backend/src/external/interface/PixelArt.External.Interface.csproj
```

Expected: `Removing migration '<timestamp>_VerifyNoSchemaChange'.` Confirm both generated files are gone.

- [ ] **Step 3: Rebuild the container**

```bash
docker compose up -d --no-deps --build api
```

Expected: `Container pixelart-api  Started`

- [ ] **Step 4: Round-trip a drawing through the real database**

This is the test unit tests cannot do — it proves the converter writes and reads the same JSON shape.

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -w "\n[HTTP %{http_code}]\n" -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"vo","width":2,"height":2,"pixels":[["#FF0000FF","#00FF00FF"],["#0000FFFF","#000000FF"]]}'
```

Expected: `[HTTP 201]` and a body whose `pixels` is `[["#FF0000FF","#00FF00FF"],["#0000FFFF","#000000FF"]]` — a nested array, **not** an object with `Width`/`Height` keys.

- [ ] **Step 5: Confirm an update still persists**

An update that changes only grid contents is the case the comparer exists for. If the comparer is wrong, this returns 204 and silently saves nothing.

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
ID=$(curl -s http://localhost:5126/api/drawings -H "Authorization: Bearer $TOKEN" | sed -E 's/.*"id":([0-9]+).*/\1/')
curl -s -o /dev/null -w "PUT -> HTTP %{http_code}\n" -X PUT "http://localhost:5126/api/drawings/$ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"vo","width":2,"height":2,"pixels":[["#FFFFFFFF","#FFFFFFFF"],["#FFFFFFFF","#FFFFFFFF"]]}'
curl -s "http://localhost:5126/api/drawings/$ID" -H "Authorization: Bearer $TOKEN"
```

Expected: `204`, then a body whose pixels are all `#FFFFFFFF`. **If the pixels come back as the original red/green/blue, the comparer is broken** — EF saw no change and skipped the UPDATE.

- [ ] **Step 6: Confirm validation is unaffected**

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/login -H "Content-Type: application/json" -d '{"username":"smoketest","password":"Passw0rd!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
curl -s -w "\n[HTTP %{http_code}]\n" -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"bad","width":5,"height":2,"pixels":[["#FF0000FF","#00FF00FF"],["#0000FFFF","#000000FF"]]}'
```

Expected: `[HTTP 400]` with `"title":"Row 0 must contain exactly 5 pixels."` — byte-identical to before this change.

- [ ] **Step 7: Report**

Give the build result, the test count, whether the throwaway migration was empty, and the actual bodies from Steps 4 to 6. Do not claim success for a step that was not run.

---

# Appendix A: Test tasks

Test code only. The spine above is complete without it.

### Test T1 — PixelGrid value object

Precedes Task 1, Step 1.

**Files:**
- Modify: `backend/src/tests/PixelArt.Core.Tests/PixelGridTests.cs`

The nine existing cases are rewritten against the new API — same behaviours, expressed as instance methods. Two are added for what the value object newly guarantees: that it cannot be mutated through the array you passed in, nor through the array it hands back.

- [ ] **Step 1: Replace the test file**

```csharp
using PixelArt.Core.Domain;

namespace PixelArt.Core.Tests;

public class PixelGridTests
{
    private static string[][] SampleRows() =>
    [
        ["#ff0000ff", "#00ff00ff"],
        ["#0000ffff", "#0a141e28"],
    ];

    private static PixelGrid Sample() => new(SampleRows());

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        Assert.False(Sample().Equals(null));
    }

    [Fact]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var grid = Sample();
        Assert.True(grid.Equals(grid));
    }

    [Fact]
    public void Equals_IdenticalContent_ReturnsTrue()
    {
        Assert.True(Sample().Equals(Sample()));
    }

    [Fact]
    public void Equals_DifferentPixel_ReturnsFalse()
    {
        var rows = SampleRows();
        rows[0][0] = "#fe0000ff";

        Assert.False(Sample().Equals(new PixelGrid(rows)));
    }

    [Fact]
    public void Equals_DifferentDimensions_ReturnsFalse()
    {
        Assert.False(Sample().Equals(new PixelGrid([["#010203ff"]])));
    }

    [Fact]
    public void GetHashCode_EqualGrids_ProduceSameHash()
    {
        Assert.Equal(Sample().GetHashCode(), Sample().GetHashCode());
    }

    [Fact]
    public void WidthAndHeight_ReflectTheRows()
    {
        var grid = Sample();

        Assert.Equal(2, grid.Width);
        Assert.Equal(2, grid.Height);
    }

    [Fact]
    public void Empty_HasZeroDimensions()
    {
        Assert.Equal(0, PixelGrid.Empty.Width);
        Assert.Equal(0, PixelGrid.Empty.Height);
    }

    [Fact]
    public void Indexer_ReturnsThePixel()
    {
        Assert.Equal("#0a141e28", Sample()[1, 1]);
    }

    [Fact]
    public void MutatingTheSourceArray_DoesNotAffectTheGrid()
    {
        var rows = SampleRows();
        var grid = new PixelGrid(rows);

        rows[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", grid[0, 0]);
    }

    [Fact]
    public void MutatingTheReturnedArray_DoesNotAffectTheGrid()
    {
        var grid = Sample();
        var copy = grid.ToArray();

        copy[0][0] = "#000000ff";

        Assert.Equal("#ff0000ff", grid[0, 0]);
    }

    [Fact]
    public void ToArray_RoundTripsThroughAnEqualGrid()
    {
        var grid = Sample();

        Assert.True(grid.Equals(new PixelGrid(grid.ToArray())));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test backend/src/PixelArt.sln`
Expected: compile failure — `PixelGrid` has no constructor, no indexer, and no `Equals(PixelGrid)`.

- [ ] **Step 3: Implement**

Go do Task 1, Step 1.

- [ ] **Step 4: Run after Task 4 completes**

`PixelGridTests` cannot pass on its own — the solution stays red until Task 4 fixes the consumers. Verify at Task 4, Step 5.

Expected then: 12 `PixelGrid` cases green, for **37 total** across the suite.

---

### Test T2 — DrawingService assertion

Precedes Task 4, Step 1.

**Files:**
- Modify: `backend/src/tests/PixelArt.Core.Tests/Drawings/DrawingServiceTests.cs`

Exactly one assertion changes. `UpdateAsync_ChangesStoredFields` reads a pixel off the entity, and the entity's `Pixels` is no longer a jagged array.

- [ ] **Step 1: Update the assertion**

Change this line:

```csharp
        Assert.Equal("#FFFFFFFF", found.Pixels[0][0]);
```

to use the value object's indexer:

```csharp
        Assert.Equal("#FFFFFFFF", found.Pixels[0, 0]);
```

Note the comma. `[0][0]` indexes an array of arrays; `[0, 0]` is the two-argument indexer on `PixelGrid`.

Nothing else in this file changes — `CreateAsync` and `UpdateAsync` still take `string[][]`, so every `Grid()` call site stays as it is.

- [ ] **Step 2: Verify at Task 4, Step 5**

This file cannot compile until Task 2 changes `Drawing.Pixels`, so there is no separate red/green cycle for it.
