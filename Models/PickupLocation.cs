using System;
using System.Collections.Generic;

namespace Zoolirante_Open_Minded.Models;

public partial class PickupLocation
{
    public int PickupLocationId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public TimeOnly? OpenFrom { get; set; }

    public TimeOnly? OpenTo { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
