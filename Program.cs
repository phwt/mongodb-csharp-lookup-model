using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace MongoLookupModel
{
    internal class Program
    {
        static IMongoCollection<Product> productsCollection;
        static IMongoCollection<Category> categoriesCollection;

        static void Main(string[] args)
        {
            var client = new MongoClient("mongodb://admin:password@localhost:27017");
            var database = client.GetDatabase("store");

            var camelCaseConventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConventionPack, type => true);

            productsCollection = database.GetCollection<Product>("products");
            categoriesCollection = database.GetCollection<Category>("categories");

            InsertProduct();
            ListProducts();
        }

        static void ListProducts()
        {
            var products = productsCollection.Aggregate()
                                             .Match(Builders<Product>.Filter.Where(_ => true))
                                             .Lookup(categoriesCollection,
                                                     p => p.CategoryId,
                                                     c => c.Id,
                                                     (Product p) => p.Category)
                                             .Unwind(p => p.Category, new AggregateUnwindOptions<Product>() { PreserveNullAndEmptyArrays = true })
                                             .ToList();

            System.Console.WriteLine(JsonSerializer.Serialize(products, new JsonSerializerOptions() { WriteIndented = true }));
        }

        static void InsertProduct()
        {
            var product = new Product()
            {
                Price = 1000,
                CategoryId = "6207eadbef1ae25be4c731b0",
                Category = new Category { Name = "Electronics" }
            };
            productsCollection.InsertOne(product);
        }
    }

    [BsonIgnoreExtraElements]
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Price { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string CategoryId { get; set; }

        [BsonIgnoreIfNull]
        public Category Category { get; set; }
    }

    public class Category
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
    }
}