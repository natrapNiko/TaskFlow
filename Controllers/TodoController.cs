using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Controllers;

public class TodoController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.TodoItems.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.CreatedAt).ToListAsync();
        return View(items);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TodoItem item)
    {
        if (!ModelState.IsValid) return View(item);
        item.CreatedAt = DateTime.UtcNow;
        db.TodoItems.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TodoItem item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid) return View(item);
        db.Update(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is not null)
        {
            db.TodoItems.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();
        item.IsCompleted = !item.IsCompleted;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
