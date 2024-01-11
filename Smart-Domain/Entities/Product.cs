using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Domain.Entities;

[NotMapped]
public class Product
{
    public int ProductId { get; set; }
    public string ProductType { get; set; }
    public string ProductName {get; set;}
    public string ProductDescription {get; set;}
    [NotMapped]
    public Media Media {get; set;}
    public string ProductCapacity { get; set;}
    public decimal ProductPrice { get; set;}
    public string ProductIngredient { get; set;}
}