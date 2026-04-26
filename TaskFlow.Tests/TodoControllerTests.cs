using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Controllers;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Tests;

public class TodoControllerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TodoController _controller;

    public TodoControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _controller = new TodoController(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Index_ReturnsViewWithAllItems()
    {
        _db.TodoItems.AddRange(
            new TodoItem { Title = "Task A" },
            new TodoItem { Title = "Task B" }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.Index(null, null, null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TodoIndexViewModel>(view.Model);
        Assert.Equal(2, vm.Items.Count());
    }

    [Fact]
    public async Task Index_SearchFiltersItems()
    {
        _db.TodoItems.AddRange(
            new TodoItem { Title = "Buy milk" },
            new TodoItem { Title = "Write report" }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.Index("milk", null, null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TodoIndexViewModel>(view.Model);
        Assert.Single(vm.Items);
        Assert.Equal("Buy milk", vm.Items.First().Title);
    }

    [Fact]
    public async Task Index_PriorityFilterWorks()
    {
        _db.TodoItems.AddRange(
            new TodoItem { Title = "High task", Priority = Priority.High },
            new TodoItem { Title = "Low task", Priority = Priority.Low }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.Index(null, null, Priority.High, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TodoIndexViewModel>(view.Model);
        Assert.Single(vm.Items);
        Assert.Equal(Priority.High, vm.Items.First().Priority);
    }

    [Fact]
    public async Task Index_PaginationReturnsCorrectPage()
    {
        for (int i = 1; i <= 8; i++)
            _db.TodoItems.Add(new TodoItem { Title = $"Task {i}" });
        await _db.SaveChangesAsync();

        var result = await _controller.Index(null, null, null, null, 2);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<TodoIndexViewModel>(view.Model);
        Assert.Equal(3, vm.Items.Count());
        Assert.Equal(2, vm.CurrentPage);
        Assert.Equal(2, vm.TotalPages);
    }

    [Fact]
    public async Task Create_ValidItem_RedirectsToIndex()
    {
        var item = new TodoItem { Title = "New Task" };

        var result = await _controller.Create(item);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, await _db.TodoItems.CountAsync());
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Title", "Required");
        var item = new TodoItem { Title = "" };

        var result = await _controller.Create(item);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, await _db.TodoItems.CountAsync());
    }

    [Fact]
    public async Task Edit_ExistingItem_UpdatesAndRedirects()
    {
        var item = new TodoItem { Title = "Original" };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();

        item.Title = "Updated";
        var result = await _controller.Edit(item.Id, item);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Updated", (await _db.TodoItems.FindAsync(item.Id))!.Title);
    }

    [Fact]
    public async Task Delete_ExistingItem_RemovesAndRedirects()
    {
        var item = new TodoItem { Title = "To Delete" };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();

        var result = await _controller.DeleteConfirmed(item.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(0, await _db.TodoItems.CountAsync());
    }

    [Fact]
    public async Task ToggleComplete_FlipsIsCompleted()
    {
        var item = new TodoItem { Title = "Toggle Me", IsCompleted = false };
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();

        await _controller.ToggleComplete(item.Id);

        Assert.True((await _db.TodoItems.FindAsync(item.Id))!.IsCompleted);
    }

    [Fact]
    public async Task Details_NonExistingItem_ReturnsNotFound()
    {
        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateCategory_ValidCategory_RedirectsToCategories()
    {
        var category = new Category { Name = "Work" };

        var result = await _controller.CreateCategory(category);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Categories", redirect.ActionName);
        Assert.Equal(1, await _db.Categories.CountAsync());
    }
}
