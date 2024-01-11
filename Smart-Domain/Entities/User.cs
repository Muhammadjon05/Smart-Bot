using System.ComponentModel.DataAnnotations;

namespace Smart_Domain.Entities;

public class User
{
    
    [Key]
    public int Id { get; set; }
    [Required]
    public long ChatId { get; set; }
    public int Step { get; set; }
    public int? AdminStep { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public bool? IsAdmin { get; set; }
    public long? AdminChatId { get; set; }
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int NumberOfOrders { get; set; }
    public int CurrentProductId { get; set; }
    public int CartId { get; set; }
    public virtual Cart Cart { get; set; }
    
}