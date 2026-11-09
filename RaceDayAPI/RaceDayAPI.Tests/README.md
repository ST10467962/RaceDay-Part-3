# RaceDayAPI Unit Tests

Unit tests for the RaceDay API (Part 2), written with xUnit, FluentAssertions and an in-memory Entity Framework Core database.

The test project covers **one controller, `CategoriesController`**, and tests **all five of its methods** with **21 tests**.

## Project Structure

```
RaceDayAPI.Tests/
├── Controllers/
│   └── CategoriesControllerTests.cs      # 21 unit tests for CategoriesController
├── RaceDayAPI.Tests.csproj               # Test project configuration
└── README.md                             # This file
```

## Quick Start

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 (or VS Code / Rider)

### Run all tests
Visual Studio: **Test → Run All Tests** (Test Explorer: Ctrl+E, T)

Command line:
```bash
dotnet test
```

### Run only the Categories tests
```bash
dotnet test --filter "CategoriesControllerTests"
```

### Run the tests for one controller method
```bash
dotnet test --filter "FullyQualifiedName~PostCategory"
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.6.6 | Testing framework |
| xunit.runner.visualstudio | 2.5.4 | Visual Studio Test Explorer integration |
| Moq | 4.20.69 | Mocking library (not used by the Categories tests) |
| FluentAssertions | 6.12.0 | Readable assertions |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | In-memory database for testing |

## Controller Under Test: CategoriesController

The five controller methods and the tests that cover them:

| # | Controller method | HTTP | Tests |
|---|-------------------|------|-------|
| 1 | `GetCategories()` | GET `api/Categories` | 3 |
| 2 | `GetCategory(id)` | GET `api/Categories/{id}` | 3 |
| 3 | `PostCategory(category)` | POST `api/Categories` | 4 |
| 4 | `PutCategory(id, category)` | PUT `api/Categories/{id}` | 6 |
| 5 | `DeleteCategory(id)` | DELETE `api/Categories/{id}` | 5 |
| | **Total** | | **21** |

### 1. GetCategories (3 tests)
- `GetCategories_WithNoData_ReturnsEmptyList`
- `GetCategories_WithData_ReturnsAllCategories`
- `GetCategories_WithLargeDataset_ReturnsAll` (100 records)

### 2. GetCategory (3 tests)
- `GetCategory_WithValidId_ReturnsCorrectCategory`
- `GetCategory_WithInvalidId_ReturnsNotFound`
- `GetCategory_WithValidId_ReturnsCategoryWithCorrectProperties`

### 3. PostCategory (4 tests)
- `PostCategory_WithValidCategory_ReturnsCreatedAtActionResult`
- `PostCategory_WithValidCategory_SavesToDatabase`
- `PostCategory_WithMultipleCategories_AllAreSaved`
- `PostCategory_WithNegativeDistance_StillSaves`

### 4. PutCategory (6 tests)
- `PutCategory_WithValidCategory_UpdatesSuccessfully` (NoContent)
- `PutCategory_WithValidCategory_SavesChangesToDatabase`
- `PutCategory_WithMismatchedId_ReturnsBadRequest`
- `PutCategory_WithNonExistentId_ReturnsNotFound`
- `PutCategory_WithMultipleUpdates_LastUpdateWins`
- `PutCategory_UpdateAllProperties_SavesCorrectly`

### 5. DeleteCategory (5 tests)
- `DeleteCategory_WithValidId_ReturnsNoContent`
- `DeleteCategory_WithValidId_RemovesFromDatabase`
- `DeleteCategory_WithInvalidId_ReturnsNotFound`
- `DeleteCategory_WithMultipleCategories_OnlyDeletesCorrectOne`
- `DeleteCategory_FollowedByGet_ReturnsNotFound`

## Latest Test Result

```
Test run finished: 21 Tests (21 Passed, 0 Failed, 0 Skipped)
```

## Test Architecture

### In-memory database isolation
Every test creates its own database with a unique name, so tests never affect each other:
```csharp
private RaceDayDbContext GetInMemoryDbContext()
{
    var options = new DbContextOptionsBuilder<RaceDayDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    return new RaceDayDbContext(options);
}
```

### Factory methods
- `CreateTestCategory()` - standard test category
- `CreateTestEvent()` - standard test event (a category belongs to an event)

### Arrange-Act-Assert
```csharp
[Fact]
public async Task GetCategory_WithInvalidId_ReturnsNotFound()
{
    // Arrange
    var context = GetInMemoryDbContext();
    var controller = new CategoriesController(context);

    // Act
    var result = await controller.GetCategory(999);

    // Assert
    result.Result.Should().BeOfType<NotFoundResult>();
}
```

## Notes and Lessons Learned

These came up while getting the suite to pass:

- **`CreatedAtAction` results:** `PostCategory` returns `CreatedAtAction(...)`, so the saved category is in `CreatedAtActionResult.Value`. `result.Value` on the `ActionResult<Category>` is `null` in that case.
- **Entity tracking in `PutCategory` tests:** the controller attaches the category it receives. If the test's own context is still tracking an object with the same key, EF Core throws "another instance with the same key is already being tracked". The tests call `context.ChangeTracker.Clear()` (or detach the entity) before calling `PutCategory`.
- **Non-existent id on `PutCategory`:** the controller catches `DbUpdateConcurrencyException` and returns `NotFoundResult`.
- **Authorization attributes:** `[Authorize]` and `[Authorize(Roles = "Organiser")]` are not enforced when a controller is called directly in a unit test. They would need integration tests.
- **Scope:** only `CategoriesController` is unit tested. The Auth tests were removed because they need a full `HttpContext` with authentication services.

## Debugging Tests

### Visual Studio
1. Right-click a test → **Debug Test**
2. Or set a breakpoint and run the test

### Test Explorer
- View → Test Explorer
- If the list is empty, click **Clear all filters**
- Use **Group By → Class** to see `CategoriesControllerTests` and its tests

### Command line
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Related Files

- `Controllers/CategoriesControllerTests.cs` - the test class
- `RaceDayAPI/Controllers/CategoriesController.cs` - code being tested

---

**Student:** Ntsakiso Chobede
**Module:** Programming 2B (PROG6212)
**Test framework:** xUnit 2.6.6
**.NET version:** 8.0