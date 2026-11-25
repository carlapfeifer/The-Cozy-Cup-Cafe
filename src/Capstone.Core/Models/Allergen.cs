namespace Capstone.Core.Models;

[Flags]
public enum Allergen
{
    None = 0,
    Gluten = 1,
    Milk = 2,
    Eggs = 4,
    Nuts = 8,
    Soy = 16,
    Fish = 32,
    Sesame = 64
}