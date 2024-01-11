using Smart_Domain.Entities;

namespace Smart_Bot.Interfaces;

public interface IProductManager
{
    ValueTask<Product> GetProductByName(string productName);
    ValueTask<Product> GetProductById(int productId);
    ValueTask<List<Product>> GetAllProducts();
    ValueTask<List<Product>> GetProductsByType(string productType);
}