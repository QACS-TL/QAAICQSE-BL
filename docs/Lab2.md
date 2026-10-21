# Lab 2 - Testing and refactoring AI code

**Goal:** reinforce AI output with human-driven validation.

**Challenge project:** E-commerce price calculator with dynamic discounts.

**Scenario:** Create a pricing engine that handles multiple discount types, tax calculations, and promotional rules.

## AI prompting phase

Ask AI for a `PricingEngine` with:

1. `CalculateItemPrice(item, quantity, customerTier)`
2. `ApplyDiscounts(cart, promotionalCodes)`
3. `CalculateTax(subtotal, location, itemCategories)`
4. `ProcessBulkDiscount(items, quantity)`
5. `ValidatePromotionalCode(code, customerHistory)`

## Human testing and validation

1. Generate unit tests with AI
2. Run them
3. Look for missing scenarios:
   - Negative quantities or prices
   - Invalid promotional codes
   - Tax-exempt items
   - Discount stacking
   - Currency precision
   - Concurrent discount application

## In this repo

- `Lab 2/Exemplar` is the happy-path engine. Its tests only cover a premium price and one discount code.
- `Lab 2/Improved` adds decimal money, tier discounts, minimum order amounts, fixed discounts, and tax exemptions.
- Run the suites with `dotnet test`.

The improved tests are a direct port of the Python course. One of them, fixed-discount capping on a `NEWUSER` code, fails in the original as well: `NEWUSER` requires a 100 minimum order, and the test uses a 10 order, so no discount is applied.

## Success metrics

- Meaningful tests, not only the happy path
- Edge cases identified and handled
- Code passes static analysis
