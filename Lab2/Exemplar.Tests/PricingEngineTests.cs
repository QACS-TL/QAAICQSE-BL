namespace Lab2.Exemplar.Tests;

public class PricingEngineTests
{
    [Fact]
    public void CalculateItemPrice_PremiumTier_AppliesFivePercentDiscount()
    {
        var engine = new PricingEngine();
        var item = new Dictionary<string, double> { ["price"] = 100 };

        var result = engine.CalculateItemPrice(item, 2, "premium");

        Assert.Equal(190d, result, precision: 5);
    }

    [Fact]
    public void ApplyDiscounts_Save10_ReducesTotalByTenPercent()
    {
        var engine = new PricingEngine();
        var cart = new List<IReadOnlyDictionary<string, double>>
        {
            new Dictionary<string, double> { ["price"] = 100, ["quantity"] = 1 }
        };

        var result = engine.ApplyDiscounts(cart, new[] { "SAVE10" });

        Assert.Equal(90d, result, precision: 5);
    }
}
