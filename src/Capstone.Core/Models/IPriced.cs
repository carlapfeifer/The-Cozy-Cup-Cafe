namespace Capstone.Core.Models;

public interface IPriced
{
    Money Price { get; }

    decimal FinalPrice(TimeOnly time);
}