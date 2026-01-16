namespace Zoolirante_Open_Minded.ViewModels;

public class LtoFilterVM
{
    public string? Category { get; set; } // optional filter
    public string? Search { get; set; }
}

public class LtoItemVM
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }

    public int Stock { get; set; }
    public int ShelfCapacity { get; set; }
    public int CurrentShelf { get; set; }

    public int Needed { get; set; }   // = max(0, ShelfCapacity - CurrentShelf)
    public int Bulk { get; set; }     // = max(0, Stock - CurrentShelf)
    public int FillQty { get; set; }  // = min(Needed, Bulk)
}

public class LtoSummaryVM
{
    public int NumArticles { get; set; }
    public int NumGroups { get; set; }        // categories
    public int TotalEA { get; set; }          // sum of FillQty
    public LtoFilterVM Filter { get; set; } = new();
}

public class LtoGroupVM
{
    public string GroupKey { get; set; } = "";   // Category
    public int NumArticles { get; set; }
    public int TotalEA { get; set; }
}

public class LtoPickVM
{
    public string GroupKey { get; set; } = "";   // Category
    public List<LtoItemVM> Lines { get; set; } = new();
}

public class LtoPickPostVM
{
    public string GroupKey { get; set; } = "";
    public List<PickLine> Lines { get; set; } = new();
    public class PickLine { public int ProductId { get; set; } public int Qty { get; set; } }
}
