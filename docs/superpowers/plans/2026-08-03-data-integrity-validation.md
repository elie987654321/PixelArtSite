# Data Integrity Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the PixelArt API reject structurally invalid drawings and weak auth payloads at the edge, so that `Width`, `Height`, and `Pixels` can never disagree and no unbounded payload reaches the database.

**Architecture:** Validation lives on the request DTOs. Scalar rules (`Name` length, dimension ranges, password length) are DataAnnotations attributes. The cross-field grid invariant — "`Pixels` is exactly `Height` rows of exactly `Width` valid colours" — is `IValidatableObject.Validate()`, which no attribute can express. `[ApiController]` converts any failure into a 400 `ValidationProblemDetails` automatically, so no controller error-handling code is needed. A `[RequestSizeLimit]` guards the one hole DataAnnotations cannot cover: the body is deserialized *before* validation runs.

**Tech Stack:** .NET 9, ASP.NET Core MVC, `System.ComponentModel.DataAnnotations` (BCL — no new packages), xUnit, Angular 19.

## Global Constraints

- **No new NuGet dependencies.** Everything used here is in the BCL or already referenced.
- **Dimension limits:** `MinSide = 1`, `MaxSide = 256`. `MaxCells = MaxSide * MaxSide = 65_536` is *derived*, not independently enforced — with a 256/side cap it is mathematically unreachable, so no validation branch checks it. It exists for error messages, storage math, and as the single number to change if `MaxSide` ever moves.
- **Colour format:** `#RRGGBBAA` only — exactly 8 hex digits, alpha always explicit. 6-digit `#RRGGBB` is **rejected**. Case-insensitive on input, **normalised to UPPERCASE on write** (`#FF0000FF`).
- **Name limit:** 1–100 characters after trimming; whitespace-only is rejected.
- **Username:** 3–50 chars, `[A-Za-z0-9_-]` only. **Password:** 8–72 chars.
- **Login must never carry strength rules** — only presence checks. See Task 4.
- **One error per category.** On a malformed grid, report the first problem and stop. A 256×256 grid of bad values must not produce 65,536 error entries.
- Target framework `net9.0`; C# collection expressions (`[nameof(X)]`) are available.
- All commands run from the repo root: `c:\dev\Formation\SitePixelArt\PixelArtSite`.

## Case handled at two layers, on purpose

Requiring explicit alpha means the stored grid has exactly one representation per colour, and it matches what the client already emits: `PencilTool` and `BrushTool` produce `ctx.color + 'ff'`, and `TRANSPARENT` is `'#00000000'` ([tool.ts:3](../../../frontend/src/app/main/editor/pixel-editor/tool.ts#L3)). Nothing in the app produces a 6-digit value, so accepting one would only widen what the database has to hold.

Case is a separate axis, and this plan addresses it in two places that solve **different** problems:

| Layer | Change | Problem it solves |
|---|---|---|
| Comparison (Task 6) | `PixelGrid.AreEqual` / `ComputeHashCode` become case-insensitive | EF stops seeing a changed grid when only case differs — including for rows **already** in the database, which normalisation can never reach |
| Write path (Tasks 1–3) | `TryNormalize` uppercases on write | Everything written from now on has one representation, so ordinal comparisons *elsewhere* stay correct |

That second row is not redundancy. The frontend already compares colours ordinally — `pixel === TRANSPARENT` in [pixel-editor.component.ts:74](../../../frontend/src/app/main/editor/pixel-editor/pixel-editor.component.ts#L74). That particular comparison happens to be safe because `#00000000` has no letters, but it shows the pattern: a case-insensitive database comparer does nothing for a client doing `===`.

**If you want the leaner version, say so.** Task 6 alone is enough to fix EF. Dropping normalisation would turn `TryNormalize` into a plain `IsValid`, delete `NormalizedPixels()` (~20 lines plus a full second grid allocation on every save), and shrink Task 3 to just the `[RequestSizeLimit]`. The cost is mixed-case data in the column forever, and the next ordinal comparison anyone writes becomes a latent bug. I recommend keeping both, but it is a real trade and it is yours to make.

## File Structure

**Create:**

| File | Responsibility |
|---|---|
| `backend/PixelArt.Api/Validation/DrawingLimits.cs` | Every numeric limit, in one place. Referenced by attributes, messages, and tests. |
| `backend/PixelArt.Api/Validation/PixelColor.cs` | The single definition of "is this a valid colour" + normalisation. Used by both the validator and the writer, so they can never disagree. |
| `backend/PixelArt.Api.Tests/Validation/ValidationHelper.cs` | Test-only shim that runs DataAnnotations the same way MVC does. |
| `backend/PixelArt.Api.Tests/Validation/PixelColorTests.cs` | Format/normalisation tests. |
| `backend/PixelArt.Api.Tests/Validation/DrawingCreateDtoValidationTests.cs` | Grid invariant tests. |
| `backend/PixelArt.Api.Tests/Validation/AuthRequestValidationTests.cs` | Auth rule tests. |

**Modify:**

| File | Change |
|---|---|
| `backend/PixelArt.Api/Dtos/Request/DrawingCreateRequest.cs` | Add attributes, `IValidatableObject`, `NormalizedPixels()`, `NormalizedName`. |
| `backend/PixelArt.Api/Dtos/Request/RegisterRequest.cs` | Add username/password rules. |
| `backend/PixelArt.Api/Dtos/Request/LoginRequest.cs` | Add presence-only rules. |
| `backend/PixelArt.Api/Controllers/DrawingsController.cs` | Write normalised values; add `[RequestSizeLimit]`. |
| `backend/PixelArt.Api/Domain/PixelGridUtility.cs` | Case-insensitive `AreEqual` and `ComputeHashCode` (Task 6). |
| `backend/PixelArt.Api.Tests/PixelGridTests.cs` | Cover the case-insensitive behaviour and the hash-code contract. |
| `frontend/src/app/main/editor/drawing-options/drawing-options.component.ts` | `max` 4096 → 256; surface server errors. |
| `frontend/src/app/main/editor/drawing-editor/drawing-editor.component.ts` | Surface server errors. |

## Out of scope (deliberately)

- A SQL `CHECK` constraint on `LEN(Pixels)`. The API is the only writer; a DB constraint would duplicate the rule with no second writer to defend against.
- Moving anything into `PixelArt.Domain`. That empty project is a separate cleanup.
- Response DTOs, pagination, the `create` route guard, 401 handling. Separate problems.
- BCrypt's 72-*byte* truncation for multi-byte passwords — see the note in Task 4.

---

### Task 1: Colour format and limits

**Files:**
- Create: `backend/PixelArt.Api/Validation/DrawingLimits.cs`
- Create: `backend/PixelArt.Api/Validation/PixelColor.cs`
- Test: `backend/PixelArt.Api.Tests/Validation/PixelColorTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `PixelArt.Api.Validation.DrawingLimits` — `const int MinSide = 1`, `MaxSide = 256`, `MaxCells = 65_536`, `MaxNameLength = 100`.
  - `PixelArt.Api.Validation.PixelColor` — `const string Transparent = "#00000000"`; `static bool TryNormalize(string? value, out string normalized)`.

- [ ] **Step 1: Write the failing test**

Create `backend/PixelArt.Api.Tests/Validation/PixelColorTests.cs`:

```csharp
using PixelArt.Api.Validation;
using Xunit;

namespace PixelArt.Api.Tests.Validation;

public class PixelColorTests
{
    [Theory]
    [InlineData("#FF0000FF", "#FF0000FF")]
    [InlineData("#00000000", "#00000000")]
    [InlineData("#0a141e28", "#0A141E28")]   // lowercase is uppercased
    [InlineData("#abcdefab", "#ABCDEFAB")]
    [InlineData("#aBcDeFaB", "#ABCDEFAB")]   // mixed case too
    public void TryNormalize_ValidColour_ReturnsUppercase(
        string input, string expected)
    {
        Assert.True(PixelColor.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#ff0000")]       // 6 digits: alpha is mandatory
    [InlineData("#ABCDEF")]
    [InlineData("ff0000ff")]      // missing '#'
    [InlineData("#ff000")]        // 5 digits
    [InlineData("#ff00000")]      // 7 digits
    [InlineData("#ff0000fff")]    // 9 digits
    [InlineData("#gg0000ff")]     // not hex
    [InlineData("#ff 000f")]      // whitespace
    [InlineData("red")]
    [InlineData("javascript:alert(1)")]
    public void TryNormalize_InvalidColour_ReturnsFalse(string? input)
    {
        Assert.False(PixelColor.TryNormalize(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void Transparent_IsItselfValidAndUnchanged()
    {
        Assert.True(PixelColor.TryNormalize(PixelColor.Transparent, out var normalized));
        Assert.Equal(PixelColor.Transparent, normalized);
    }

    [Fact]
    public void MaxCells_IsDerivedFromMaxSide()
    {
        Assert.Equal(DrawingLimits.MaxSide * DrawingLimits.MaxSide, DrawingLimits.MaxCells);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~PixelColorTests"`
Expected: FAIL to compile — `The type or namespace name 'Validation' does not exist in the namespace 'PixelArt.Api'`.

- [ ] **Step 3: Write minimal implementation**

Create `backend/PixelArt.Api/Validation/DrawingLimits.cs`:

```csharp
namespace PixelArt.Api.Validation;

// Every size limit the API enforces, in one place.
public static class DrawingLimits
{
    public const int MinSide = 1;

    public const int MaxSide = 256;

    // Derived, not separately enforced: with MaxSide = 256 a drawing cannot
    // exceed this, so no validation branch checks it. Used for messages and
    // for reasoning about worst-case row size (~1.5 MB as nvarchar).
    public const int MaxCells = MaxSide * MaxSide;

    public const int MaxNameLength = 100;
}
```

Create `backend/PixelArt.Api/Validation/PixelColor.cs`:

```csharp
namespace PixelArt.Api.Validation;

// The single definition of a valid pixel colour. Both the request validator
// and the code that writes to the database go through TryNormalize, so what
// we accept and what we store can never drift apart.
public static class PixelColor
{
    public const string Transparent = "#00000000";

    // "#RRGGBBAA" — alpha is mandatory, which is what the editor already
    // emits. A 6-digit "#RRGGBB" is rejected rather than assumed opaque.
    private const int RgbaLength = 9;

    // Accepts "#RRGGBBAA" in any case; `normalized` is the uppercase form.
    // Collapsing case matters because consumers outside EF compare colours
    // ordinally, so "#FF0000FF" and "#ff0000ff" would read as different.
    // Character scanning rather than a Regex: this runs once per pixel, up to
    // DrawingLimits.MaxCells times per request.
    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (value is null) return false;
        if (value.Length != RgbaLength) return false;
        if (value[0] != '#') return false;

        for (var i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i])) return false;
        }

        normalized = value.ToUpperInvariant();
        return true;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~PixelColorTests"`
Expected: PASS — 19 tests.

- [ ] **Step 5: Commit**

```bash
git add backend/PixelArt.Api/Validation backend/PixelArt.Api.Tests/Validation
git commit -m "feat(validation): add pixel colour format rules and drawing limits"
```

---

### Task 2: Grid invariant on DrawingCreateDto

**Files:**
- Modify: `backend/PixelArt.Api/Dtos/Request/DrawingCreateRequest.cs` (whole file replaced)
- Create: `backend/PixelArt.Api.Tests/Validation/ValidationHelper.cs`
- Test: `backend/PixelArt.Api.Tests/Validation/DrawingCreateDtoValidationTests.cs`

**Interfaces:**
- Consumes: `DrawingLimits.MinSide/MaxSide/MaxNameLength`, `PixelColor.TryNormalize` from Task 1.
- Produces: `DrawingCreateDto` implementing `IValidatableObject`, plus `string NormalizedName { get; }` and `string[][] NormalizedPixels()` — both consumed by `DrawingsController` in Task 3.

**Critical ordering fact — read before writing tests.** `Validator.TryValidateObject(..., validateAllProperties: true)` validates property attributes first and **returns early if any failed**, without calling `IValidatableObject.Validate()`. MVC's model binder behaves identically. Two consequences:

1. Inside `Validate()` you may assume `Width`/`Height` are already within range. Do not re-check them.
2. A DTO with `Width = 0` *and* a mismatched grid yields **only** the `Range` error. Tests must expect that, not both errors.

This ordering is also a safety property: an oversized `Width` is rejected before any code walks the grid.

- [ ] **Step 1: Write the failing test**

Create `backend/PixelArt.Api.Tests/Validation/ValidationHelper.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace PixelArt.Api.Tests.Validation;

internal static class ValidationHelper
{
    // Runs DataAnnotations the way MVC's model binder does: property
    // attributes first, and IValidatableObject.Validate() only if they pass.
    public static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);
        return results;
    }

    public static bool HasErrorFor(this IReadOnlyList<ValidationResult> results, string member) =>
        results.Any(r => r.MemberNames.Contains(member));
}
```

Create `backend/PixelArt.Api.Tests/Validation/DrawingCreateDtoValidationTests.cs`:

```csharp
using PixelArt.Api.Dtos;
using PixelArt.Api.Validation;
using Xunit;

namespace PixelArt.Api.Tests.Validation;

public class DrawingCreateDtoValidationTests
{
    // A well-formed 2x2 drawing. Individual tests break one thing at a time.
    private static DrawingCreateDto Valid() => new()
    {
        Name = "Sprite",
        Width = 2,
        Height = 2,
        Pixels =
        [
            ["#ff0000ff", "#00ff00ff"],
            ["#0000ffff", "#00000000"],
        ],
    };

    private static string[][] Grid(int width, int height) =>
        Enumerable.Range(0, height)
            .Select(_ => Enumerable.Repeat("#ff0000ff", width).ToArray())
            .ToArray();

    [Fact]
    public void ValidDrawing_ProducesNoErrors()
    {
        Assert.Empty(ValidationHelper.Validate(Valid()));
    }

    [Fact]
    public void PixelsWithWrongRowCount_IsRejected()
    {
        var dto = Valid();
        dto.Pixels = Grid(width: 2, height: 3);   // Height says 2

        var results = ValidationHelper.Validate(dto);

        Assert.True(results.HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Fact]
    public void PixelsWithWrongRowWidth_IsRejected()
    {
        var dto = Valid();
        dto.Pixels = [["#ff0000ff", "#00ff00ff"], ["#0000ffff"]];   // row 1 short

        var results = ValidationHelper.Validate(dto);

        Assert.True(results.HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Fact]
    public void PixelsWithInvalidColour_IsRejected()
    {
        var dto = Valid();
        dto.Pixels[1][0] = "not-a-colour";

        var results = ValidationHelper.Validate(dto);

        Assert.True(results.HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Fact]
    public void PixelsWithSixDigitColour_IsRejected()
    {
        // Alpha is mandatory: "#FF0000" is not a shorthand, it is invalid.
        var dto = Valid();
        dto.Pixels[0][0] = "#FF0000";

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Fact]
    public void PixelsWithUppercaseColour_IsAccepted()
    {
        var dto = Valid();
        dto.Pixels[0][0] = "#FF0000FF";

        Assert.Empty(ValidationHelper.Validate(dto));
    }

    [Fact]
    public void EmptyPixels_IsRejected()
    {
        var dto = Valid();
        dto.Pixels = [];

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(DrawingLimits.MaxSide + 1)]
    [InlineData(4096)]
    public void OutOfRangeWidth_IsRejected(int width)
    {
        var dto = Valid();
        dto.Width = width;

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Width)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(DrawingLimits.MaxSide + 1)]
    public void OutOfRangeHeight_IsRejected(int height)
    {
        var dto = Valid();
        dto.Height = height;

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Height)));
    }

    [Fact]
    public void MaximumSizeDrawing_IsAccepted()
    {
        var dto = new DrawingCreateDto
        {
            Name = "Big",
            Width = DrawingLimits.MaxSide,
            Height = DrawingLimits.MaxSide,
            Pixels = Grid(DrawingLimits.MaxSide, DrawingLimits.MaxSide),
        };

        Assert.Empty(ValidationHelper.Validate(dto));
    }

    [Fact]
    public void OversizedDimension_IsRejectedWithoutWalkingTheGrid()
    {
        // Attributes run before Validate(), so a 4096-wide claim is refused
        // before any per-pixel work happens. Only the Width error appears.
        var dto = new DrawingCreateDto
        {
            Name = "Huge",
            Width = 4096,
            Height = 4096,
            Pixels = [],
        };

        var results = ValidationHelper.Validate(dto);

        Assert.True(results.HasErrorFor(nameof(DrawingCreateDto.Width)));
        Assert.False(results.HasErrorFor(nameof(DrawingCreateDto.Pixels)));
    }

    [Fact]
    public void MalformedGrid_ReportsOneErrorNotOnePerPixel()
    {
        var dto = new DrawingCreateDto
        {
            Name = "Bad",
            Width = 64,
            Height = 64,
            Pixels = Enumerable.Range(0, 64)
                .Select(_ => Enumerable.Repeat("nope", 64).ToArray())
                .ToArray(),
        };

        Assert.Single(ValidationHelper.Validate(dto));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_IsRejected(string name)
    {
        var dto = Valid();
        dto.Name = name;

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Name)));
    }

    [Fact]
    public void OverlongName_IsRejected()
    {
        var dto = Valid();
        dto.Name = new string('x', DrawingLimits.MaxNameLength + 1);

        Assert.True(ValidationHelper.Validate(dto).HasErrorFor(nameof(DrawingCreateDto.Name)));
    }

    [Fact]
    public void NormalizedName_IsTrimmed()
    {
        var dto = Valid();
        dto.Name = "  Sprite  ";

        Assert.Equal("Sprite", dto.NormalizedName);
    }

    [Fact]
    public void NormalizedPixels_UppercasesColours()
    {
        var dto = Valid();
        dto.Pixels[0][0] = "#ff0000ff";
        dto.Pixels[0][1] = "#00Ff00fF";

        var normalized = dto.NormalizedPixels();

        Assert.Equal("#FF0000FF", normalized[0][0]);
        Assert.Equal("#00FF00FF", normalized[0][1]);
    }

    [Fact]
    public void NormalizedPixels_DoesNotMutateTheRequest()
    {
        var dto = Valid();
        dto.Pixels[0][0] = "#ff0000ff";

        dto.NormalizedPixels();

        Assert.Equal("#ff0000ff", dto.Pixels[0][0]);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~DrawingCreateDtoValidationTests"`
Expected: FAIL to compile — `'DrawingCreateDto' does not contain a definition for 'NormalizedName'`.

- [ ] **Step 3: Write minimal implementation**

Replace `backend/PixelArt.Api/Dtos/Request/DrawingCreateRequest.cs` entirely:

```csharp
using System.ComponentModel.DataAnnotations;
using PixelArt.Api.Validation;

namespace PixelArt.Api.Dtos;

// Input model for creating/updating a Drawing.
//
// Width, Height and Pixels are three fields describing one thing, so they can
// disagree. Validate() is what makes them agree: no attribute can express a
// rule that spans several properties.
public class DrawingCreateDto : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(DrawingLimits.MaxNameLength, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Range(DrawingLimits.MinSide, DrawingLimits.MaxSide)]
    public int Width { get; set; }

    [Range(DrawingLimits.MinSide, DrawingLimits.MaxSide)]
    public int Height { get; set; }

    [Required]
    public string[][] Pixels { get; set; } = [];

    // Trimmed name. Safe to read once validation has passed.
    public string NormalizedName => Name.Trim();

    // The grid in canonical uppercase 8-digit form. Only valid to call after
    // validation has passed — every cell is assumed to parse. Returns a fresh
    // array; the request object is left untouched.
    public string[][] NormalizedPixels()
    {
        var rows = new string[Pixels.Length][];

        for (var y = 0; y < Pixels.Length; y++)
        {
            var source = Pixels[y];
            var row = new string[source.Length];

            for (var x = 0; x < source.Length; x++)
            {
                PixelColor.TryNormalize(source[x], out var normalized);
                row[x] = normalized;
            }

            rows[y] = row;
        }

        return rows;
    }

    // Runs only after every attribute above has passed, so Width and Height
    // are already known to be within [MinSide, MaxSide]. Each check bails on
    // the first failure: a 256x256 grid must not yield 65,536 error entries.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Name must not be blank.",
                [nameof(Name)]);
        }

        if (Pixels.Length != Height)
        {
            yield return new ValidationResult(
                $"Pixels must contain exactly {Height} rows to match Height, but contained {Pixels.Length}.",
                [nameof(Pixels)]);
            yield break;
        }

        for (var y = 0; y < Pixels.Length; y++)
        {
            var row = Pixels[y];

            if (row is null)
            {
                yield return new ValidationResult(
                    $"Pixels row {y} must not be null.",
                    [nameof(Pixels)]);
                yield break;
            }

            if (row.Length != Width)
            {
                yield return new ValidationResult(
                    $"Pixels row {y} must contain exactly {Width} values to match Width, but contained {row.Length}.",
                    [nameof(Pixels)]);
                yield break;
            }

            for (var x = 0; x < row.Length; x++)
            {
                if (!PixelColor.TryNormalize(row[x], out _))
                {
                    yield return new ValidationResult(
                        $"Pixels[{y}][{x}] must be a colour of the form #RRGGBBAA, but was '{row[x]}'.",
                        [nameof(Pixels)]);
                    yield break;
                }
            }
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~DrawingCreateDtoValidationTests"`
Expected: PASS — 22 tests.

Then confirm nothing regressed: `dotnet test backend/PixelArt.sln`
Expected: PASS — all tests including the 9 pre-existing `PixelGridTests`.

- [ ] **Step 5: Commit**

```bash
git add backend/PixelArt.Api/Dtos/Request/DrawingCreateRequest.cs backend/PixelArt.Api.Tests/Validation
git commit -m "feat(validation): enforce Width/Height/Pixels consistency on drawing requests"
```

---

### Task 3: Wire the controller to normalised values and cap the request body

**Files:**
- Modify: `backend/PixelArt.Api/Controllers/DrawingsController.cs:44-77`

**Interfaces:**
- Consumes: `DrawingCreateDto.NormalizedName`, `DrawingCreateDto.NormalizedPixels()` from Task 2.
- Produces: nothing new.

**Why the size limit is a separate concern.** Model binding deserializes the JSON body *before* validation runs. A 200 MB body is fully parsed into memory and only then rejected by `[Range]`. DataAnnotations structurally cannot prevent that; only a body-size cap can. The largest legitimate payload is `MaxCells` × ~12 bytes ≈ 786 KB, so 2 MB is generous. Kestrel's global default is 30 MB — far looser than this endpoint needs.

- [ ] **Step 1: Add the size limit and normalisation to `Create`**

In `backend/PixelArt.Api/Controllers/DrawingsController.cs`, replace the `Create` action:

```csharp
    // POST: api/drawings
    // 2 MB ceiling: the largest valid drawing (DrawingLimits.MaxCells cells at
    // ~12 bytes each) is under 1 MB. The body is deserialized before any
    // validation attribute runs, so this is the only thing standing between an
    // oversized payload and the parser.
    [HttpPost]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<Drawing>> Create(DrawingCreateDto input)
    {
        var drawing = new Drawing
        {
            Name = input.NormalizedName,
            Width = input.Width,
            Height = input.Height,
            Pixels = input.NormalizedPixels(),
            UserId = CurrentUserId
        };

        _db.Drawings.Add(drawing);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = drawing.Id }, drawing);
    }
```

- [ ] **Step 2: Apply the same to `Update`**

Replace the `Update` action:

```csharp
    // PUT: api/drawings/5
    [HttpPut("{id}")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Update(int id, DrawingCreateDto input)
    {
        var drawing = await _db.Drawings
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == CurrentUserId);
        if (drawing is null) return NotFound();

        drawing.Name = input.NormalizedName;
        drawing.Width = input.Width;
        drawing.Height = input.Height;
        drawing.Pixels = input.NormalizedPixels();

        await _db.SaveChangesAsync();
        return NoContent();
    }
```

Note: resizing an existing drawing stays allowed. The validated DTO guarantees the new `Width`/`Height`/`Pixels` are mutually consistent, so the stored row is coherent either way. Whether resize *should* be permitted is a product question, not an integrity one.

- [ ] **Step 3: Verify it compiles and the suite still passes**

Run: `dotnet build backend/PixelArt.sln`
Expected: Build succeeded, 0 errors.

Run: `dotnet test backend/PixelArt.sln`
Expected: PASS — all tests.

- [ ] **Step 4: Verify by hand against a running API**

Start the stack: `docker compose up -d --build`

Register and capture a token:

```bash
TOKEN=$(curl -s -X POST http://localhost:5126/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"username":"integrity_probe","password":"correct-horse"}' \
  | sed -E 's/.*"token":"([^"]+)".*/\1/')
```

Reject a grid that disagrees with its dimensions — expect **400** with a `Pixels` entry in `errors`:

```bash
curl -i -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"bad","width":2,"height":2,"pixels":[["#ff0000ff"]]}'
```

Reject an oversized canvas — expect **400** with a `Width` entry:

```bash
curl -i -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"huge","width":4096,"height":4096,"pixels":[]}'
```

Reject a 6-digit colour — expect **400**, since alpha is mandatory:

```bash
curl -i -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"noalpha","width":1,"height":1,"pixels":[["#ff0000"]]}'
```

Accept a valid drawing written in lowercase — expect **201**, and confirm the response body shows `#FF0000FF`, not `#ff0000ff`:

```bash
curl -i -X POST http://localhost:5126/api/drawings \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"name":"good","width":2,"height":1,"pixels":[["#ff0000ff","#00000000"]]}'
```

- [ ] **Step 5: Commit**

```bash
git add backend/PixelArt.Api/Controllers/DrawingsController.cs
git commit -m "feat(api): store normalised pixel data and cap drawing request bodies"
```

---

### Task 4: Auth request rules

**Files:**
- Modify: `backend/PixelArt.Api/Dtos/Request/RegisterRequest.cs`
- Modify: `backend/PixelArt.Api/Dtos/Request/LoginRequest.cs`
- Test: `backend/PixelArt.Api.Tests/Validation/AuthRequestValidationTests.cs`

**Interfaces:**
- Consumes: `ValidationHelper` from Task 2.
- Produces: nothing consumed by later tasks.

**Two rules that are easy to get wrong:**

1. **`LoginRequest` gets presence checks only — never strength rules.** If the minimum ever rises to 12, every existing user with a 10-character password would receive a 400 at login instead of being able to sign in. It also tells an attacker the password policy for free. Presence only.
2. **Password max is 72, and that number is not arbitrary.** bcrypt's algorithm ignores input past 72 bytes, and `BCrypt.Net-Next` truncates silently — so without a cap, two different 100-character passwords sharing a 72-byte prefix would authenticate interchangeably. Capping at 72 makes the truncation impossible to hit. *Caveat:* `StringLength` counts UTF-16 characters, not UTF-8 bytes, so a password of 72 multi-byte characters can still exceed 72 bytes. Fully closing that needs byte-length validation or `EnhancedHashPassword` (which SHA-384 pre-hashes); both are out of scope here.

- [ ] **Step 1: Write the failing test**

Create `backend/PixelArt.Api.Tests/Validation/AuthRequestValidationTests.cs`:

```csharp
using PixelArt.Api.Dtos;
using Xunit;

namespace PixelArt.Api.Tests.Validation;

public class AuthRequestValidationTests
{
    private static RegisterRequest ValidRegistration() => new()
    {
        Username = "pixel_artist",
        Password = "correct-horse",
    };

    [Fact]
    public void ValidRegistration_ProducesNoErrors()
    {
        Assert.Empty(ValidationHelper.Validate(ValidRegistration()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]                       // shorter than 3
    [InlineData("has space")]
    [InlineData("has.dot")]
    [InlineData("<script>")]
    [InlineData("  padded  ")]
    public void InvalidUsername_IsRejected(string username)
    {
        var request = ValidRegistration();
        request.Username = username;

        Assert.True(ValidationHelper.Validate(request)
            .HasErrorFor(nameof(RegisterRequest.Username)));
    }

    [Fact]
    public void OverlongUsername_IsRejected()
    {
        var request = ValidRegistration();
        request.Username = new string('a', 51);

        Assert.True(ValidationHelper.Validate(request)
            .HasErrorFor(nameof(RegisterRequest.Username)));
    }

    [Theory]
    [InlineData("a_b")]
    [InlineData("Pixel-Artist-99")]
    [InlineData("___")]
    public void AcceptableUsername_IsAllowed(string username)
    {
        var request = ValidRegistration();
        request.Username = username;

        Assert.Empty(ValidationHelper.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]                  // 7 chars, one below the floor
    public void WeakPassword_IsRejected(string password)
    {
        var request = ValidRegistration();
        request.Password = password;

        Assert.True(ValidationHelper.Validate(request)
            .HasErrorFor(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void PasswordBeyondBcryptLimit_IsRejected()
    {
        // bcrypt ignores input past 72 bytes; refuse it rather than
        // silently truncate.
        var request = ValidRegistration();
        request.Password = new string('x', 73);

        Assert.True(ValidationHelper.Validate(request)
            .HasErrorFor(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void PasswordAtBcryptLimit_IsAccepted()
    {
        var request = ValidRegistration();
        request.Password = new string('x', 72);

        Assert.Empty(ValidationHelper.Validate(request));
    }

    [Fact]
    public void Login_RequiresBothFields()
    {
        var results = ValidationHelper.Validate(new LoginRequest());

        Assert.True(results.HasErrorFor(nameof(LoginRequest.Username)));
        Assert.True(results.HasErrorFor(nameof(LoginRequest.Password)));
    }

    [Fact]
    public void Login_DoesNotApplyRegistrationStrengthRules()
    {
        // A user who registered under an older, laxer policy must still be
        // able to sign in. Login validates presence, nothing more.
        var request = new LoginRequest { Username = "ab", Password = "old" };

        Assert.Empty(ValidationHelper.Validate(request));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~AuthRequestValidationTests"`
Expected: FAIL — `InvalidUsername_IsRejected`, `WeakPassword_IsRejected`, `Login_RequiresBothFields` and others fail because no rules exist yet.

- [ ] **Step 3: Write minimal implementation**

Replace `backend/PixelArt.Api/Dtos/Request/RegisterRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace PixelArt.Api.Dtos;

// Credentials supplied when creating an account.
public class RegisterRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(
        "^[A-Za-z0-9_-]+$",
        ErrorMessage = "Username may contain only letters, digits, underscores and hyphens.")]
    public string Username { get; set; } = string.Empty;

    // Upper bound is bcrypt's: it ignores input past 72 bytes, and
    // BCrypt.Net truncates silently rather than failing. Refusing longer
    // input keeps two distinct passwords from becoming interchangeable.
    [Required(AllowEmptyStrings = false)]
    [StringLength(72, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
```

Replace `backend/PixelArt.Api/Dtos/Request/LoginRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace PixelArt.Api.Dtos;

// Credentials supplied when logging in.
//
// Presence checks only — deliberately no length or charset rules. Applying
// the registration policy here would lock out anyone who signed up under an
// earlier, laxer policy, and would leak that policy to unauthenticated
// callers. Wrong credentials are rejected by the password check, not by
// model validation.
public class LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string Username { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~AuthRequestValidationTests"`
Expected: PASS — 18 tests.

Run the whole suite: `dotnet test backend/PixelArt.sln`
Expected: PASS.

- [ ] **Step 5: Verify the registration path by hand**

Empty password is now refused with **400** rather than creating an account:

```bash
curl -i -X POST http://localhost:5126/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"username":"weakling","password":""}'
```

The `AuthController.Register` call to `input.Username.Trim()` ([AuthController.cs:29](../../../backend/PixelArt.Api/Controllers/AuthController.cs#L29)) is now a no-op, since the charset rule already rejects any username containing whitespace. Leave it — it costs nothing and keeps the controller correct if the rule is ever relaxed.

- [ ] **Step 6: Commit**

```bash
git add backend/PixelArt.Api/Dtos/Request/RegisterRequest.cs backend/PixelArt.Api/Dtos/Request/LoginRequest.cs backend/PixelArt.Api.Tests/Validation/AuthRequestValidationTests.cs
git commit -m "feat(validation): add username and password rules to registration"
```

---

### Task 5: Align the client and surface server validation errors

**Files:**
- Modify: `frontend/src/app/main/editor/drawing-options/drawing-options.component.ts:22`, `:57-61`
- Modify: `frontend/src/app/main/editor/drawing-editor/drawing-editor.component.ts:69-73`

**Interfaces:**
- Consumes: the 400 `ValidationProblemDetails` shape produced by Tasks 2–4.
- Produces: nothing.

**Why this belongs in this plan.** The client currently offers canvases up to 4096 per side. Leaving that alone means the form happily accepts 1000×1000 and the save fails with `'Could not save the drawing.'` after the user has drawn. Shipping the server rule without this change would introduce that dead end.

- [ ] **Step 1: Lower the client-side dimension ceiling**

In `frontend/src/app/main/editor/drawing-options/drawing-options.component.ts`, change line 22:

```typescript
  readonly max = 256;   // must match DrawingLimits.MaxSide on the API
```

- [ ] **Step 2: Add a shared error reader to the options component**

Add the import at the top of `drawing-options.component.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
```

Add this private method to `DrawingOptionsComponent`:

```typescript
  // A 400 from the API carries ValidationProblemDetails: an `errors` object
  // keyed by field name. Show what the server actually objected to instead of
  // a generic failure message.
  private describeSaveError(err: unknown): string {
    const problem = (err as HttpErrorResponse)?.error;
    const errors = problem?.errors as Record<string, string[]> | undefined;

    if (errors) {
      const messages = Object.values(errors).flat();
      if (messages.length > 0) return messages.join(' ');
    }

    return 'Could not save the drawing.';
  }
```

Replace the existing `onError` handler inside `onSave` (currently lines 57-61):

```typescript
    const onError = (err: unknown) => {
      console.error(err);
      this.saving = false;
      this.saveError = this.describeSaveError(err);
    };
```

- [ ] **Step 3: Do the same in the editor component**

Add the import at the top of `frontend/src/app/main/editor/drawing-editor/drawing-editor.component.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
```

Add the same method to `ExistingDrawingEditorWrapper`:

```typescript
  private describeSaveError(err: unknown): string {
    const problem = (err as HttpErrorResponse)?.error;
    const errors = problem?.errors as Record<string, string[]> | undefined;

    if (errors) {
      const messages = Object.values(errors).flat();
      if (messages.length > 0) return messages.join(' ');
    }

    return 'Could not save the drawing.';
  }
```

Replace the `error` callback inside `onSave` (currently lines 69-73):

```typescript
      error: (err) => {
        console.error(err);
        this.saving = false;
        this.saveError = this.describeSaveError(err);
      },
```

- [ ] **Step 4: Verify the build and the round trip**

Run: `cd frontend && npm run build`
Expected: build succeeds with no TypeScript errors.

Then, with the stack running (`docker compose up -d --build`), in the browser:
1. Open `/create` and try width `300` — the form's own validation now refuses it before any request is sent.
2. Create a valid 16×16 drawing, draw a few pixels, save — expect success.
3. Confirm the gallery renders it, proving normalised `#rrggbbaa` values still paint correctly on the canvas.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/main/editor/drawing-options/drawing-options.component.ts frontend/src/app/main/editor/drawing-editor/drawing-editor.component.ts
git commit -m "feat(frontend): match API dimension limit and show server validation errors"
```

---

### Task 6: Case-insensitive grid comparison

**Files:**
- Modify: `backend/PixelArt.Api/Domain/PixelGridUtility.cs:22-24` (`AreEqual` inner loop), `:36-39` (`ComputeHashCode` inner loop)
- Test: `backend/PixelArt.Api.Tests/PixelGridTests.cs` (append to the existing class)

**Interfaces:**
- Consumes: nothing.
- Produces: no signature changes. `PixelGrid.AreEqual` and `PixelGrid.ComputeHashCode` keep their shapes; only their semantics change.

**Independent of Tasks 1–5.** It touches no file they touch and can be done first, last, or on its own.

**The contract that makes this dangerous to half-do.** These two methods are handed to EF as a `ValueComparer` ([AppDbContext.cs:35-38](../../../backend/PixelArt.Api/Data/AppDbContext.cs#L35-L38)). `ValueComparer` inherits the standard .NET equality contract: **any two values that compare equal must return the same hash code.** Change `AreEqual` alone and `#FF0000FF` / `#ff0000ff` become equal-but-differently-hashed. EF uses the hash for snapshot bookkeeping, so the result is change tracking that behaves inconsistently depending on which path a comparison takes — a far nastier bug than the spurious UPDATE being fixed. Both methods move in the same commit, and the test below exists specifically to pin that down.

- [ ] **Step 1: Write the failing tests**

Append to `backend/PixelArt.Api.Tests/PixelGridTests.cs`, inside the existing `PixelGridTests` class:

```csharp
    [Fact]
    public void AreEqual_SameColoursDifferentCase_ReturnsTrue()
    {
        string[][] lower = [["#ff0000ff", "#0a141e28"]];
        string[][] upper = [["#FF0000FF", "#0A141E28"]];

        Assert.True(PixelGrid.AreEqual(lower, upper));
    }

    [Fact]
    public void ComputeHashCode_SameColoursDifferentCase_ProduceSameHash()
    {
        // The ValueComparer contract: anything AreEqual calls equal MUST hash
        // identically. This test is the reason the two methods change together.
        string[][] lower = [["#ff0000ff", "#0a141e28"]];
        string[][] upper = [["#FF0000FF", "#0A141E28"]];

        Assert.Equal(
            PixelGrid.ComputeHashCode(lower),
            PixelGrid.ComputeHashCode(upper));
    }

    [Fact]
    public void AreEqual_NullPixelVersusValue_ReturnsFalse()
    {
        string[][] withNull = [[null!]];
        string[][] withValue = [["#ff0000ff"]];

        Assert.False(PixelGrid.AreEqual(withNull, withValue));
    }

    [Fact]
    public void ComputeHashCode_GridWithNullPixel_DoesNotThrow()
    {
        // StringComparer.OrdinalIgnoreCase.GetHashCode(null) throws, so this
        // pins down that HashCode.Add null-checks before calling the comparer.
        string[][] grid = [[null!, "#ff0000ff"]];

        Assert.Null(Record.Exception(() => PixelGrid.ComputeHashCode(grid)));
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~PixelGridTests"`

Expected: `AreEqual_SameColoursDifferentCase_ReturnsTrue` FAILS with `Assert.True() Failure`, and `ComputeHashCode_SameColoursDifferentCase_ProduceSameHash` FAILS with `Assert.Equal() Failure` showing two different integers.

`AreEqual_NullPixelVersusValue_ReturnsFalse` and `ComputeHashCode_GridWithNullPixel_DoesNotThrow` should already PASS against the current ordinal implementation — that is expected and correct. They are regression guards for Step 3, not new behaviour. Do not "fix" them.

- [ ] **Step 3: Make the comparison case-insensitive**

In `backend/PixelArt.Api/Domain/PixelGridUtility.cs`, update the class comment and both inner loops.

Replace the type comment:

```csharp
// Structural compare/hash/copy for a jagged pixel grid (string[][] of hex colours),
// used by EF's value comparer to track changes without serializing to JSON.
//
// Colours compare case-insensitively: "#FF0000FF" and "#ff0000ff" are the same
// colour, so a grid differing only in case is not a change worth saving. AreEqual
// and ComputeHashCode MUST agree on this — EF's ValueComparer requires equal
// values to hash equally.
```

In `AreEqual`, replace the innermost comparison:

```csharp
            for (var x = 0; x < rowA.Length; x++)
            {
                if (!string.Equals(rowA[x], rowB[x], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
```

In `ComputeHashCode`, replace the pixel accumulation:

```csharp
            foreach (var pixel in row)
            {
                hash.Add(pixel, StringComparer.OrdinalIgnoreCase);
            }
```

`HashCode.Add<T>(T value, IEqualityComparer<T>? comparer)` checks for null before calling the comparer, which is what keeps `ComputeHashCode_GridWithNullPixel_DoesNotThrow` green.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test backend/PixelArt.sln --filter "FullyQualifiedName~PixelGridTests"`
Expected: PASS — 13 tests (9 pre-existing + 4 new).

Run the whole suite: `dotnet test backend/PixelArt.sln`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/PixelArt.Api/Domain/PixelGridUtility.cs backend/PixelArt.Api.Tests/PixelGridTests.cs
git commit -m "fix(data): compare pixel grids case-insensitively in EF value comparer"
```

---

## Verification checklist

Run after all six tasks. Do not report completion until each line has been executed and observed.

- [ ] `dotnet build backend/PixelArt.sln` — 0 errors, 0 warnings.
- [ ] `dotnet test backend/PixelArt.sln` — all tests pass. Expected count: 13 `PixelGridTests` (9 pre-existing + 4 from Task 6) + 19 (Task 1) + 22 (Task 2) + 18 (Task 4) = **72**.
- [ ] `PixelGrid.AreEqual` and `PixelGrid.ComputeHashCode` agree on case — the pair of Task 6 tests both pass. A green `AreEqual` test with a red hash test means a broken `ValueComparer`, not partial progress.
- [ ] `cd frontend && npm run build` — succeeds.
- [ ] `POST /api/drawings` with `width: 2, height: 2, pixels: [["#ff0000ff"]]` → **400**, `errors.Pixels` present.
- [ ] `POST /api/drawings` with `width: 4096` → **400**, `errors.Width` present.
- [ ] `POST /api/drawings` with a 6-digit colour `#ff0000` → **400**, `errors.Pixels` present.
- [ ] `POST /api/drawings` with a valid 2×1 grid using `#ff0000ff` → **201**, response body contains `#FF0000FF`.
- [ ] `POST /api/auth/register` with `password: ""` → **400**, no user row created.
- [ ] `POST /api/auth/login` with a 3-character password → **401** (credentials wrong), *not* 400. Confirms login carries no strength rules.
- [ ] Browser: create, draw, save, and view a 16×16 drawing end to end.

## What this plan does not fix

Named so they aren't mistaken for oversights:

- The duplicated `d.UserId == CurrentUserId` ownership check in `DrawingsController` — still four hand-written copies, still the highest-severity issue in the codebase.
- Entities returned directly as API responses; `GET /api/drawings` still returns every full grid.
- The committed SA password in `appsettings.Development.json`.
- The unguarded `/create` route and the missing 401 handling on expired tokens.
