using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using Xunit;

namespace RaceDayAPI.Tests.Controllers
{
    public class CategoriesControllerTests
    {
        private RaceDayDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new RaceDayDbContext(options);
        }

        private Category CreateTestCategory(int eventId = 1)
        {
            return new Category
            {
                CategoryName = "Test Category",
                Distance = 10.5m,
                EntryFee = 150.00m,
                AgeGroup = "18-30",
                EventId = eventId
            };
        }

        private Event CreateTestEvent()
        {
            return new Event
            {
                EventName = "Test Event",
                EventDate = DateTime.UtcNow.AddDays(7),
                Location = "Test Location",
                Description = "Test Description",
                StartTime = new TimeSpan(09, 00, 00),
                EndTime = new TimeSpan(17, 00, 00),
                CreatedAt = DateTime.UtcNow,
                OrganiserId = 1
            };
        }

        // ============ CREATE (POST) TESTS ============

        [Fact]
        public async Task PostCategory_WithValidCategory_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.PostCategory(testCategory);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = (CreatedAtActionResult)result.Result!;
            createdResult.ActionName.Should().Be("GetCategory");
            createdResult.RouteValues!["id"].Should().Be(testCategory.CategoryId);
            // The saved category is in createdResult.Value (result.Value is null for CreatedAtAction)
            createdResult.Value.Should().BeEquivalentTo(testCategory);
        }

        [Fact]
        public async Task PostCategory_WithValidCategory_SavesToDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Act
            await controller.PostCategory(testCategory);

            // Assert
            var savedCategory = await context.Categories.FindAsync(testCategory.CategoryId);
            savedCategory.Should().NotBeNull();
            savedCategory?.CategoryName.Should().Be("Test Category");
            savedCategory?.Distance.Should().Be(10.5m);
            savedCategory?.EntryFee.Should().Be(150.00m);
        }

        [Fact]
        public async Task PostCategory_WithMultipleCategories_AllAreSaved()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var category1 = CreateTestCategory(testEvent.EventId);
            var category2 = new Category
            {
                CategoryName = "Category 2",
                Distance = 21.0m,
                EntryFee = 200.00m,
                AgeGroup = "31-50",
                EventId = testEvent.EventId
            };

            // Act
            await controller.PostCategory(category1);
            await controller.PostCategory(category2);

            // Assert
            var allCategories = await context.Categories.ToListAsync();
            allCategories.Should().HaveCount(2);
        }

        // ============ READ (GET) TESTS ============

        [Fact]
        public async Task GetCategories_WithNoData_ReturnsEmptyList()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);

            // Act
            var result = await controller.GetCategories();

            // Assert
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCategories_WithData_ReturnsAllCategories()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var category1 = CreateTestCategory(testEvent.EventId);
            var category2 = new Category
            {
                CategoryName = "Category 2",
                Distance = 21.0m,
                EntryFee = 200.00m,
                AgeGroup = "31-50",
                EventId = testEvent.EventId
            };

            context.Events.Add(testEvent);
            context.Categories.Add(category1);
            context.Categories.Add(category2);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetCategories();

            // Assert
            result.Value.Should().HaveCount(2);
            result.Value?.Should().Contain(c => c.CategoryName == "Test Category");
            result.Value?.Should().Contain(c => c.CategoryName == "Category 2");
        }

        [Fact]
        public async Task GetCategory_WithValidId_ReturnsCorrectCategory()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetCategory(testCategory.CategoryId);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value?.CategoryId.Should().Be(testCategory.CategoryId);
            result.Value?.CategoryName.Should().Be("Test Category");
        }

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

        [Fact]
        public async Task GetCategory_WithValidId_ReturnsCategoryWithCorrectProperties()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetCategory(testCategory.CategoryId);

            // Assert
            result.Value.Should().NotBeNull();
            result.Value?.CategoryName.Should().Be("Test Category");
            result.Value?.Distance.Should().Be(10.5m);
            result.Value?.EntryFee.Should().Be(150.00m);
            result.Value?.AgeGroup.Should().Be("18-30");
        }

        // ============ UPDATE (PUT) TESTS ============

        [Fact]
        public async Task PutCategory_WithValidCategory_UpdatesSuccessfully()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            var categoryId = testCategory.CategoryId;
            var eventId = testCategory.EventId;

            // Detach the original to avoid tracking conflicts
            context.Entry(testCategory).State = EntityState.Detached;

            var controller = new CategoriesController(context);
            var updatedCategory = new Category
            {
                CategoryId = categoryId,
                CategoryName = "Updated Category Name",
                Distance = 15.5m,
                EntryFee = testCategory.EntryFee,
                AgeGroup = testCategory.AgeGroup,
                EventId = eventId
            };

            // Act
            var result = await controller.PutCategory(categoryId, updatedCategory);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task PutCategory_WithValidCategory_SavesChangesToDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            var categoryId = testCategory.CategoryId;
            var eventId = testCategory.EventId;

            // Detach the original to avoid tracking conflicts
            context.Entry(testCategory).State = EntityState.Detached;

            var controller = new CategoriesController(context);
            var updatedCategory = new Category
            {
                CategoryId = categoryId,
                CategoryName = "Updated Category Name",
                Distance = 15.5m,
                EntryFee = 250.00m,
                AgeGroup = testCategory.AgeGroup,
                EventId = eventId
            };

            // Act
            await controller.PutCategory(categoryId, updatedCategory);

            // Assert
            var categoryFromDb = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            categoryFromDb.Should().NotBeNull();
            categoryFromDb?.CategoryName.Should().Be("Updated Category Name");
            categoryFromDb?.Distance.Should().Be(15.5m);
            categoryFromDb?.EntryFee.Should().Be(250.00m);
        }

        [Fact]
        public async Task PutCategory_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testCategory = CreateTestCategory();

            // Act
            var result = await controller.PutCategory(1, testCategory);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public async Task PutCategory_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);
            testCategory.CategoryId = 999;

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.PutCategory(999, testCategory);

            // Assert - When entity doesn't exist, should return NotFound from DbUpdateConcurrencyException catch
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task PutCategory_WithMultipleUpdates_LastUpdateWins()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            var categoryId = testCategory.CategoryId;
            var eventId = testCategory.EventId;

            // Stop tracking the entity added above so the controller can attach its own instance
            context.ChangeTracker.Clear();

            var controller = new CategoriesController(context);

            // First update
            var firstUpdate = new Category
            {
                CategoryId = categoryId,
                CategoryName = "First Update",
                Distance = 10.5m,
                EntryFee = 150.00m,
                AgeGroup = "18-30",
                EventId = eventId
            };
            await controller.PutCategory(categoryId, firstUpdate);

            // Clear tracked entities again before the second update
            context.ChangeTracker.Clear();

            // Verify first update
            var firstCheck = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            firstCheck?.CategoryName.Should().Be("First Update");

            // Second update
            var secondUpdate = new Category
            {
                CategoryId = categoryId,
                CategoryName = "Second Update",
                Distance = 10.5m,
                EntryFee = 150.00m,
                AgeGroup = "18-30",
                EventId = eventId
            };
            await controller.PutCategory(categoryId, secondUpdate);

            // Assert
            var categoryFromDb = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            categoryFromDb?.CategoryName.Should().Be("Second Update");
        }

        // ============ DELETE TESTS ============

        [Fact]
        public async Task DeleteCategory_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.DeleteCategory(testCategory.CategoryId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteCategory_WithValidId_RemovesFromDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            // Act
            await controller.DeleteCategory(testCategory.CategoryId);

            // Assert
            var categoryFromDb = await context.Categories.FindAsync(testCategory.CategoryId);
            categoryFromDb.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCategory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);

            // Act
            var result = await controller.DeleteCategory(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteCategory_WithMultipleCategories_OnlyDeletesCorrectOne()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var category1 = CreateTestCategory(testEvent.EventId);
            var category2 = new Category
            {
                CategoryName = "Category 2",
                Distance = 21.0m,
                EntryFee = 200.00m,
                AgeGroup = "31-50",
                EventId = testEvent.EventId
            };

            context.Events.Add(testEvent);
            context.Categories.Add(category1);
            context.Categories.Add(category2);
            await context.SaveChangesAsync();

            var idToDelete = category1.CategoryId;

            // Act
            await controller.DeleteCategory(idToDelete);

            // Assert
            var allCategories = await context.Categories.ToListAsync();
            allCategories.Should().HaveCount(1);
            allCategories[0].CategoryId.Should().Be(category2.CategoryId);
        }

        // ============ EDGE CASES & VALIDATION TESTS ============

        [Fact]
        public async Task PostCategory_WithNegativeDistance_StillSaves()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = new Category
            {
                CategoryName = "Invalid Category",
                Distance = -5.0m,
                EntryFee = 100.00m,
                AgeGroup = "All",
                EventId = testEvent.EventId
            };

            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.PostCategory(testCategory);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var savedCategory = await context.Categories.FindAsync(testCategory.CategoryId);
            savedCategory?.Distance.Should().Be(-5.0m);
        }

        [Fact]
        public async Task GetCategories_WithLargeDataset_ReturnsAll()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            context.Events.Add(testEvent);
            await context.SaveChangesAsync();

            var categories = Enumerable.Range(1, 100)
                .Select(i => new Category
                {
                    CategoryName = $"Category {i}",
                    Distance = i * 1.5m,
                    EntryFee = i * 10m,
                    AgeGroup = $"{i * 5}-{i * 5 + 20}",
                    EventId = testEvent.EventId
                })
                .ToList();

            foreach (var category in categories)
            {
                context.Categories.Add(category);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await controller.GetCategories();

            // Assert
            result.Value.Should().HaveCount(100);
        }

        [Fact]
        public async Task PutCategory_UpdateAllProperties_SavesCorrectly()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            var categoryId = testCategory.CategoryId;
            var eventId = testCategory.EventId;

            // Detach the original to avoid tracking conflicts
            context.Entry(testCategory).State = EntityState.Detached;

            var controller = new CategoriesController(context);
            var updatedCategory = new Category
            {
                CategoryId = categoryId,
                CategoryName = "Completely New Name",
                Distance = 99.99m,
                EntryFee = 999.99m,
                AgeGroup = "99+",
                EventId = eventId
            };

            // Act
            await controller.PutCategory(categoryId, updatedCategory);

            // Assert
            context.Entry(testCategory).State = EntityState.Detached;
            var categoryFromDb = await context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            categoryFromDb.Should().NotBeNull();
            categoryFromDb?.CategoryName.Should().Be("Completely New Name");
            categoryFromDb?.Distance.Should().Be(99.99m);
            categoryFromDb?.EntryFee.Should().Be(999.99m);
            categoryFromDb?.AgeGroup.Should().Be("99+");
        }

        [Fact]
        public async Task DeleteCategory_FollowedByGet_ReturnsNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = new CategoriesController(context);
            var testEvent = CreateTestEvent();
            var testCategory = CreateTestCategory(testEvent.EventId);

            context.Events.Add(testEvent);
            context.Categories.Add(testCategory);
            await context.SaveChangesAsync();

            // Act
            await controller.DeleteCategory(testCategory.CategoryId);
            var result = await controller.GetCategory(testCategory.CategoryId);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }
    }
}