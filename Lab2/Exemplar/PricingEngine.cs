namespace Lab2.Exemplar;

/// <summary>
/// Typical AI-generated pricing engine for Lab 2.
/// Happy-path only: no validation, no tax, and discounts compound on the running total.
/// </summary>
public class PricingEngine
{
    private readonly Dictionary<string, double> _taxRates = new()
    {
        ["US"] = 0.08,
        ["CA"] = 0.12
    };

    private readonly Dictionary<string, double> _discountCodes = new()
    {
        ["SAVE10"] = 0.10,
        ["WELCOME"] = 0.15
    };

    public IReadOnlyDictionary<string, double> TaxRates => _taxRates;
    public IReadOnlyDictionary<string, double> DiscountCodes => _discountCodes;

    public double CalculateItemPrice(IReadOnlyDictionary<string, double> item, int quantity, string customerTier)
    {
        var basePrice = item["price"] * quantity;
        if (customerTier == "premium")
        {
            basePrice *= 0.95;
        }

        return basePrice;
    }

    public double ApplyDiscounts(IReadOnlyList<IReadOnlyDictionary<string, double>> cart, IReadOnlyList<string> promotionalCodes)
    {
        var total = cart.Sum(item => item["price"] * item["quantity"]);
        foreach (var code in promotionalCodes)
        {
            if (_discountCodes.TryGetValue(code, out var rate))
            {
                total *= 1 - rate;
            }
        }

        return total;
    }
}
