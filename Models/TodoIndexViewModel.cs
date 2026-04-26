namespace TaskFlow.Models;

public class TodoIndexViewModel
{
    public IEnumerable<TodoItem> Items { get; set; } = [];
    public IEnumerable<Category> Categories { get; set; } = [];

    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
    public Priority? PriorityFilter { get; set; }
    public int? CategoryFilter { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 5;
}
