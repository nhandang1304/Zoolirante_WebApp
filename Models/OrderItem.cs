using System;
using System.Collections.Generic;

namespace Zoolirante_Open_Minded.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Merchandise Product { get; set; } = null!;
}
