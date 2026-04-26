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

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task Index_ReturnsViewWithAllItems()
    {
        _db.TodoItems.AddRange(
            new TodoItem { Title = "Task A" },
            new TodoItem { Title = "Task B" }
        );
        await _db.SaveChangesAsync();

        var result = await _controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<TodoItem>>(view.Model);
        Assert.Equal(2, model.Count());
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

        var updated = await _db.TodoItems.FindAsync(item.Id);
        Assert.True(updated!.IsCompleted);
    }

    [Fact]
    public async Task Details_NonExistingItem_ReturnsNotFound()
    {
        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
