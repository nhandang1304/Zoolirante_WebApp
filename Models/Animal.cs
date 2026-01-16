using System;
using System.Collections.Generic;

namespace Zoolirante_Open_Minded.Models;

public partial class Animal
{
    public int AnimalId { get; set; }

    public string Name { get; set; } = null!;

    public string? Species { get; set; }

    public string? Region { get; set; }

    public string? ConservationStatus { get; set; }

    public string? Habitat { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? ExhibitLocation { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
