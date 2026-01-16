using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;



namespace Zoolirante_Open_Minded.Models;

public partial class Event
{
    public int EventId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime StartTime { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime EndTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
    public int? Capacity { get; set; }

    [Range(0, 999999.99, ErrorMessage = "Price cannot be negative")]
    public decimal Price { get; set; }

    [Required, StringLength(100)]
    public string Location { get; set; } = null!;

    [ValidateNever]               
    public string? ImageUrl { get; set; }   


    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();
    public virtual EventAnimal EventAnimal { get; set; }
}


