using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Domain.Entities;

[NotMapped]
public class Media
{
    public bool Exist { get; set; }
    public string? ImageUrl { get; set; }
}
