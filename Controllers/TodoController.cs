using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Controllers;

public class TodoController(AppDbContext db) : Controller
{
    private const int PageSize = 5;

    public async Task<IActionResult> Index(
        string? search,
        string? statusFilter,
        Priority? priorityFilter,
        int? categoryFilter,
        int page = 1)
    {
        var query = db.TodoItems.Include(t => t.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        if (statusFilter == "completed")
            query = query.Where(t => t.IsCompleted);
        else if (statusFilter == "pending")
            query = query.Where(t => !t.IsCompleted);

        if (priorityFilter.HasValue)
            query = query.Where(t => t.Priority == priorityFilter);

        if (categoryFilter.HasValue)
            query = query.Where(t => t.CategoryId == categoryFilter);

        query = query.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
        var items = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        var vm = new TodoIndexViewModel
        {
            Items = items,
            Categories = await db.Categories.ToListAsync(),
            Search = search,
            StatusFilter = statusFilter,
            PriorityFilter = priorityFilter,
            CategoryFilter = categoryFilter,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = PageSize
        };

        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TodoItem item)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name");
            return View(item);
        }
        item.CreatedAt = DateTime.UtcNow;
        db.TodoItems.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name", item.CategoryId);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TodoItem item)
    {
        if (id != item.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name", item.CategoryId);
            return View(item);
        }
        db.Update(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await db.TodoItems.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (item is null) return NotFound();
        return View(item);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.TodoItems.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
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

    // --- Category management ---
    public async Task<IActionResult> Categories() =>
        View(await db.Categories.Include(c => c.TodoItems).ToListAsync());

    public IActionResult CreateCategory() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is not null)
        {
            db.Categories.Remove(category);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Categories));
    }
}
