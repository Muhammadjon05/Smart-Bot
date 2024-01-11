using Newtonsoft.Json;
using Smart_Domain.Entities;

namespace Smart_Bot.Interfaces;

public class ProductManager : IProductManager
{
    private List<Product> products = new List<Product>();

    public ProductManager()
    {
        var path = "C:\\Users\\muham\\RiderProjects\\Smart-Bot\\Smart-Web\\wwwroot\\ProductImages\\Products.json";
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            using (StreamReader sr = new StreamReader(fs))
            {
                string json = sr.ReadToEnd();
                products = JsonConvert.DeserializeObject<List<Product>>(json);
            }
        }

    }
    
    public async ValueTask<Product> GetProductByName(string productName)
    {
        return  products.Find(p => p.ProductName.Equals(productName));
    }

    public async ValueTask<Product> GetProductById(int productId)
    {
        return products.Find(p => p.ProductId == productId);
    }

    public async ValueTask<List<Product>> GetAllProducts()
    {
        return products;
    }

    public async ValueTask<List<Product>> GetProductsByType(string productType)
    {
        return products.FindAll(p => p.ProductType.Equals(productType));
    }
}