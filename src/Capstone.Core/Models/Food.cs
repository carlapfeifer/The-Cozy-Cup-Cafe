namespace Capstone.Core.Models;

public sealed class Food : MenuItem
{
    public Food(
        string id,
        string displayName,
        Category category,
        Money price,
        Allergen allergens = Allergen.None,
        bool isAvailable = true)
        : base(id, displayName, category, price, allergens, isAvailable)
    {
        
    }
    
    public override string Describe() => 
        Allergens == Allergen.None
            ? $"{Card()} · food · no allergens"
            : $"{Card()} · food · contains {Allergens}";
    
}