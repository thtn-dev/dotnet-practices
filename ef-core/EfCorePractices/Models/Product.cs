using Microsoft.EntityFrameworkCore;


namespace EfCorePractices.Models;

public record Currency(string Code, string Name);
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public Currency Currency { get; set; } = default!;

    public override string ToString()
    {
        return $"Product {{ Id = {Id}, Name = {Name}, Price = {Price}, Currency = {Currency.Code} ({Currency.Name}) }}";
    }
}