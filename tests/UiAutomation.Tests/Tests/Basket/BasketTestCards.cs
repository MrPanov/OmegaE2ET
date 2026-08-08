namespace UiAutomation.Tests.Tests.Basket;

internal static class BasketTestCards
{
    public const string AddProduct = "5614799817";
    public const string ProductDetails = "651002";
    public const string WarehouseStocks = "46101216738";
    public const string Quantity = "3211996264";
    public const string Selection = "69501396346";
    public const string SelectAll = "4400676052";
    public const string Removal = "69101138569";
    public const string InvoiceReservation = "4610495";

    public static IReadOnlyList<string> All { get; } =
    [
        AddProduct,
        ProductDetails,
        WarehouseStocks,
        Quantity,
        Selection,
        SelectAll,
        Removal,
        InvoiceReservation
    ];

    static BasketTestCards()
    {
        if (All.Distinct(StringComparer.Ordinal).Count() != All.Count)
        {
            throw new InvalidOperationException("Every Basket/Invoice scenario must use a unique card.");
        }
    }
}
