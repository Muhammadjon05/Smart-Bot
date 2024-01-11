namespace Smart_Domain.Entities;

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; }
}