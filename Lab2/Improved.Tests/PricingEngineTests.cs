namespace Lab2.Improved.Tests;

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();
    private readonly Item _sampleItem = new("1", "Test Product", 100.00m, "electronics");

    [Fact]
    public void CalculateItemPrice_BasicTier_ChargesFullPrice()
    {
        var result = _engine.CalculateItemPrice(_sampleItem, 2, CustomerTier.Basic);

        Assert.Equal(200.00m, result);
    }

    [Fact]
    public void CalculateItemPrice_PremiumTier_AppliesFivePercentDiscount()
    {
        var result = _engine.CalculateItemPrice(_sampleItem, 2, CustomerTier.Premium);

        Assert.Equal(190.00m, result);
    }

    [Fact]
    public void CalculateItemPrice_ZeroQuantity_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            _engine.CalculateItemPrice(_sampleItem, 0, CustomerTier.Basic));

        Assert.Contains("Quantity must be positive", error.Message);
    }

    [Fact]
    public void CalculateItemPrice_NegativeQuantity_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            _engine.CalculateItemPrice(_sampleItem, -1, CustomerTier.Basic));

        Assert.Contains("Quantity must be positive", error.Message);
    }

    [Fact]
    public void ApplyDiscounts_ValidCode_ReturnsBreakdown()
    {
        var cart = new List<CartItem> { new(_sampleItem, 1) };

        var result = _engine.ApplyDiscounts(cart, new[] { "SAVE10" }, CustomerTier.Basic);

        Assert.Equal(100.00m, result.Subtotal);
        Assert.Equal(10.00m, result.DiscountAmount);
        Assert.Equal(90.00m, result.FinalTotal);
        Assert.Contains("SAVE10", result.AppliedCodes);
    }

    [Fact]
    public void ApplyDiscounts_MinimumOrderNotMet_AppliesNoDiscount()
    {
        var lowPriceItem = new Item("2", "Cheap Item", 30.00m, "misc");
        var cart = new List<CartItem> { new(lowPriceItem, 1) };

        var result = _engine.ApplyDiscounts(cart, new[] { "WELCOME" }, CustomerTier.Basic);

        Assert.Equal(0.00m, result.DiscountAmount);
        Assert.Empty(result.AppliedCodes);
    }

    [Fact]
    public void ApplyDiscounts_InvalidCode_AppliesNoDiscount()
    {
        var cart = new List<CartItem> { new(_sampleItem, 1) };

        var result = _engine.ApplyDiscounts(cart, new[] { "INVALID" }, CustomerTier.Basic);

        Assert.Equal(0.00m, result.DiscountAmount);
        Assert.Empty(result.AppliedCodes);
    }

    [Fact]
    public void ApplyDiscounts_MultipleCodes_StacksPercentageAndFixedAmount()
    {
        var expensiveItem = new Item("3", "Expensive Item", 200.00m, "luxury");
        var cart = new List<CartItem> { new(expensiveItem, 1) };

        var result = _engine.ApplyDiscounts(cart, new[] { "SAVE10", "NEWUSER" }, CustomerTier.Basic);

        var expectedDiscount = 200.00m * 0.10m + 20.00m;
        Assert.Equal(expectedDiscount, result.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscounts_EmptyCart_Throws()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            _engine.ApplyDiscounts(new List<CartItem>(), new[] { "SAVE10" }, CustomerTier.Basic));

        Assert.Contains("Cart cannot be empty", error.Message);
    }

    [Fact]
    public void CalculateTax_StandardUsRate_ChargesEightPercent()
    {
        var cart = new List<CartItem> { new(_sampleItem, 1) };

        var tax = _engine.CalculateTax(100.00m, "US", cart);

        Assert.Equal(8.00m, tax);
    }

    [Fact]
    public void CalculateTax_ExemptItems_TaxesOnlyTaxableAmount()
    {
        var taxableItem = new Item("1", "Taxable", 100.00m, "electronics");
        var exemptItem = new Item("2", "Exempt", 50.00m, "medicine", taxExempt: true);
        var cart = new List<CartItem> { new(taxableItem, 1), new(exemptItem, 1) };

        var tax = _engine.CalculateTax(150.00m, "US", cart);

        Assert.Equal(8.00m, tax);
    }

    [Fact]
    public void CalculateTax_UnknownLocation_Throws()
    {
        var cart = new List<CartItem> { new(_sampleItem, 1) };

        var error = Assert.Throws<ArgumentException>(() =>
            _engine.CalculateTax(100.00m, "INVALID", cart));

        Assert.Contains("Tax rate not available", error.Message);
    }

    [Fact]
    public void CalculateItemPrice_MaintainsDecimalPrecision()
    {
        var item = new Item("1", "Precision Test", 33.33m, "test");

        var result = _engine.CalculateItemPrice(item, 3, CustomerTier.Premium);

        Assert.Equal(94.99m, result);
    }

    [Fact]
    public void ApplyDiscounts_FixedDiscountDoesNotMakeTotalNegative()
    {
        var cheapItem = new Item("1", "Cheap", 10.00m, "test");
        var cart = new List<CartItem> { new(cheapItem, 1) };

        var result = _engine.ApplyDiscounts(cart, new[] { "NEWUSER" }, CustomerTier.Basic);

        // Same assertions as the Python lab. NEWUSER requires a 100 minimum, so this 10 order is unchanged.
        Assert.Equal(10.00m, result.FinalTotal);
        Assert.Equal(0.00m, result.DiscountAmount);
    }
}
