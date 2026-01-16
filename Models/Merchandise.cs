using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;



namespace Zoolirante_Open_Minded.Models;

public partial class Merchandise
{
    public decimal? SpecialPrice { get; set; }
    
    public int SpecialQty { get; set; } = 0;
    
    public string? SpecialReason { get; set; }   

    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    [Range(0, int.MaxValue)]
    public int ShelfCapacity { get; set; } = 0;  

    [Range(0, int.MaxValue)]
    public int CurrentShelf { get; set; } = 0;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
public class MerchandiseIndexVM
{
    public string? SelectedCategory { get; set; }
    public List<SelectListItem> Categories { get; set; } = new();
    public List<Merchandise> Items { get; set; } = new();
}