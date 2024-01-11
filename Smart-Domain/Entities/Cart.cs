namespace Smart_Domain.Entities;

public class Cart
{
    public int CartId { get; set; }
    public int UserId { get; set; }
    public DateTime DateCreated { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<CartItem> CartItems { get; set; }
    
}