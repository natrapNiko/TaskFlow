using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    string[] predefined =
    [
        "Work", "Personal", "Shopping", "Health & Fitness",
        "Finance", "Education", "Home & Cleaning",
        "Travel", "Family", "Hobbies"
    ];

    var existing = db.Categories.Select(c => c.Name).ToHashSet();

    foreach (var name in predefined.Where(n => !existing.Contains(n)))
        db.Categories.Add(new Category { Name = name });

    // Remove old categories that are not predefined and have no tasks
    var toRemove = db.Categories
        .Where(c => !predefined.Contains(c.Name) && !c.TodoItems.Any())
        .ToList();
    db.Categories.RemoveRange(toRemove);

    await db.SaveChangesAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Todo}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
