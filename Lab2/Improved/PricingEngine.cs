namespace Lab2.Improved;

public enum CustomerTier
{
    Basic,
    Premium,
    Vip
}

public enum DiscountType
{
    Percentage,
    FixedAmount,
    BuyXGetY
}

public sealed class Item
{
    public Item(string id, string name, decimal price, string category, bool taxExempt = false)
    {
        Id = id;
        Name = name;
        Price = price;
        Category = category;
        TaxExempt = taxExempt;
    }

    public string Id { get; }
    public string Name { get; }
    public decimal Price { get; }
    public string Category { get; }
    public bool TaxExempt { get; }
}

public sealed class CartItem
{
    public CartItem(Item item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public Item Item { get; }
    public int Quantity { get; }
}

public sealed class DiscountRule
{
    public DiscountRule(
        string code,
        DiscountType discountType,
        decimal value,
        decimal minOrderAmount = 0m,
        int? maxUsesPerCustomer = null,
        List<string>? validCategories = null)
    {
        Code = code;
        DiscountType = discountType;
        Value = value;
        MinOrderAmount = minOrderAmount;
        MaxUsesPerCustomer = maxUsesPerCustomer;
        ValidCategories = validCategories;
    }

    public string Code { get; }
    public DiscountType DiscountType { get; }
    public decimal Value { get; }
    public decimal MinOrderAmount { get; }
    public int? MaxUsesPerCustomer { get; }
    public List<string>? ValidCategories { get; }
}

public sealed class PricingBreakdown
{
    public required decimal Subtotal { get; init; }
    public required decimal DiscountAmount { get; init; }
    public required decimal FinalTotal { get; init; }
    public required List<string> AppliedCodes { get; init; }
}

/// <summary>
/// Reference pricing engine: decimal money, tier discounts, promotional rules, and tax exemptions.
/// </summary>
public sealed class PricingEngine
{
    private readonly Dictionary<string, decimal> _taxRates = new()
    {
        ["US"] = 0.08m,
        ["CA"] = 0.12m,
        ["UK"] = 0.20m
    };

    private readonly Dictionary<CustomerTier, decimal> _tierDiscounts = new()
    {
        [CustomerTier.Basic] = 0m,
        [CustomerTier.Premium] = 0.05m,
        [CustomerTier.Vip] = 0.10m
    };

    private readonly Dictionary<string, DiscountRule> _discountRules = new()
    {
        ["SAVE10"] = new DiscountRule("SAVE10", DiscountType.Percentage, 0.10m),
        ["WELCOME"] = new DiscountRule("WELCOME", DiscountType.Percentage, 0.15m, 50m),
        ["NEWUSER"] = new DiscountRule("NEWUSER", DiscountType.FixedAmount, 20m, 100m)
    };

    /// <summary>Calculate price for a single item with quantity and tier discount.</summary>
    public decimal CalculateItemPrice(Item item, int quantity, CustomerTier customerTier)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive");
        }

        if (item.Price < 0)
        {
            throw new ArgumentException("Item price cannot be negative");
        }

        var basePrice = item.Price * quantity;
        var tierDiscount = _tierDiscounts.GetValueOrDefault(customerTier);
        var discountedPrice = basePrice * (1m - tierDiscount);
        return decimal.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Apply discounts and return a detailed breakdown.</summary>
    public PricingBreakdown ApplyDiscounts(IReadOnlyList<CartItem> cart, IReadOnlyList<string> promotionalCodes, CustomerTier customerTier)
    {
        if (cart.Count == 0)
        {
            throw new ArgumentException("Cart cannot be empty");
        }

        var subtotal = 0m;
        foreach (var cartItem in cart)
        {
            subtotal += CalculateItemPrice(cartItem.Item, cartItem.Quantity, customerTier);
        }

        var discountAmount = 0m;
        var appliedCodes = new List<string>();
        foreach (var code in promotionalCodes)
        {
            if (!_discountRules.TryGetValue(code, out var rule))
            {
                Log.Warning($"Invalid promotional code: {code}");
                continue;
            }

            if (subtotal < rule.MinOrderAmount)
            {
                Log.Info($"Code {code} requires minimum order of {rule.MinOrderAmount}");
                continue;
            }

            decimal codeDiscount;
            if (rule.DiscountType == DiscountType.Percentage)
            {
                codeDiscount = subtotal * rule.Value;
            }
            else if (rule.DiscountType == DiscountType.FixedAmount)
            {
                codeDiscount = Math.Min(rule.Value, subtotal);
            }
            else
            {
                codeDiscount = 0m;
            }

            discountAmount += codeDiscount;
            appliedCodes.Add(code);
        }

        var finalTotal = Math.Max(0m, subtotal - discountAmount);
        return new PricingBreakdown
        {
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            FinalTotal = finalTotal,
            AppliedCodes = appliedCodes
        };
    }

    /// <summary>Calculate tax with exemption handling.</summary>
    public decimal CalculateTax(decimal subtotal, string location, IReadOnlyList<CartItem> cart)
    {
        if (!_taxRates.TryGetValue(location, out var taxRate))
        {
            throw new ArgumentException($"Tax rate not available for location: {location}");
        }

        var taxableAmount = 0m;
        foreach (var cartItem in cart)
        {
            if (!cartItem.Item.TaxExempt)
            {
                taxableAmount += cartItem.Item.Price * cartItem.Quantity;
            }
        }

        return decimal.Round(taxableAmount * taxRate, 2, MidpointRounding.AwayFromZero);
    }

    private static class Log
    {
        public static void Info(string message) => Console.Error.WriteLine($"INFO: {message}");
        public static void Warning(string message) => Console.Error.WriteLine($"WARNING: {message}");
    }
}
