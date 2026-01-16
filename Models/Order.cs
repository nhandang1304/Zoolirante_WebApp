using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Zoolirante_Open_Minded.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int PickupLocationId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }
    public string Items { get; set; }

    public string Status { get; set; } = null;

    [Required]
    [Display(Name = "Date and Time Pickup")]
    public DateTime PickupDate { get; set; }


    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PickupLocation PickupLocation { get; set; } = null;

    public virtual User User { get; set; } = null;
}
