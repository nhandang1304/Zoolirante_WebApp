using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoolirante_Open_Minded.Models;
using Zoolirante_Open_Minded.ViewModels;

public class LtoController : Controller
{
    private readonly ZooliranteDatabaseContext _db;
    public LtoController(ZooliranteDatabaseContext db) => _db = db;

    // Helpers
    private static (int needed, int bulk, int fill) Calc(Merchandise m)
    {
        var needed = Math.Max(0, m.ShelfCapacity - m.CurrentShelf);
        var bulk = Math.Max(0, m.Stock - m.CurrentShelf); // derived only
        var fill = Math.Min(needed, bulk);
        return (needed, bulk, fill);
    }
    private bool IsManager() =>
        string.Equals(HttpContext.Session.GetString("Role"), "Manager", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(HttpContext.Session.GetString("Role"), "Admin", StringComparison.OrdinalIgnoreCase);

    // GET /Lto
    public async Task<IActionResult> Index([FromQuery] LtoFilterVM f)
    {
        if (!IsManager()) return Forbid();

        var q = _db.Merchandises.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Category)) q = q.Where(x => x.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Search)) q = q.Where(x => x.Name.Contains(f.Search));

        var items = await q.ToListAsync();

        // use named tuple member .fill (not Item3) for readability
        var fills = items.Select(m => Calc(m).fill).Where(x => x > 0).ToList();

        var vm = new LtoSummaryVM
        {
            NumArticles = fills.Count,
            NumGroups = items.Where(m => Calc(m).fill > 0)
                             .Select(m => m.Category ?? "(Uncategorized)")
                             .Distinct()
                             .Count(),
            TotalEA = fills.Sum(),
            Filter = f
        };
        return View(vm);
    }

    // GET /Lto/Groups
    public async Task<IActionResult> Groups([FromQuery] LtoFilterVM f)
    {
        if (!IsManager()) return Forbid();

        var q = _db.Merchandises.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Category)) q = q.Where(x => x.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Search)) q = q.Where(x => x.Name.Contains(f.Search));

        var groups = await q.ToListAsync();
        var vm = groups
            .Select(m => new { m, c = Calc(m) })
            .Where(x => x.c.fill > 0)
            .GroupBy(x => x.m.Category ?? "(Uncategorized)")
            .Select(g => new LtoGroupVM
            {
                GroupKey = g.Key,
                NumArticles = g.Count(),
                TotalEA = g.Sum(x => x.c.fill)
            })
            .OrderByDescending(g => g.TotalEA)
            .ToList();

        ViewBag.Filter = f;
        return View(vm);
    }

    // GET /Lto/Pick?group=Category
    public async Task<IActionResult> Pick(string group)
    {
        if (!IsManager()) return Forbid();
        var items = await _db.Merchandises.AsNoTracking()
                     .Where(m => (m.Category ?? "(Uncategorized)") == (group ?? "(Uncategorized)"))
                     .ToListAsync();

        var lines = items.Select(m => {
            var (needed, bulk, fill) = Calc(m);
            return new LtoItemVM
            {
                ProductId = m.ProductId,
                Name = m.Name,
                Category = m.Category,
                ImageUrl = m.ImageUrl,
                Stock = m.Stock,
                ShelfCapacity = m.ShelfCapacity,
                CurrentShelf = m.CurrentShelf,
                Needed = needed,
                Bulk = bulk,
                FillQty = fill
            };
        }).Where(x => x.FillQty > 0).ToList();

        return View(new LtoPickVM { GroupKey = group, Lines = lines });
    }

    // POST /Lto/ApplyPick
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPick(LtoPickPostVM post)
    {
        if (!IsManager()) return Forbid();
        if (post?.Lines == null || post.Lines.Count == 0) return RedirectToAction(nameof(Groups));

        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var line in post.Lines)
        {
            if (line.Qty <= 0) continue;

            var m = await _db.Merchandises.FirstOrDefaultAsync(x => x.ProductId == line.ProductId);
            if (m == null) continue;

            var (needed, bulk, _) = Calc(m);
            var qty = Math.Min(line.Qty, Math.Min(needed, bulk));
            if (qty <= 0) continue;

            // Move from bulk -> shelf. STOCK REMAINS UNCHANGED.
            m.CurrentShelf = Math.Min(m.ShelfCapacity, m.CurrentShelf + qty);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Toast"] = "Pick applied.";
        return RedirectToAction(nameof(Pick), new { group = post.GroupKey });
    }
}
