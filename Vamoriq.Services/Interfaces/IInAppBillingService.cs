namespace Vamoriq.Services.Interfaces
{
    public interface IInAppBillingService
    {
        Task<IReadOnlyList<ProductInfo>> GetProductsAsync(IEnumerable<string> productIds, CancellationToken cancellationToken = default);
        Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PurchaseResult>> RestoreAsync(CancellationToken cancellationToken = default);
    }

    public record ProductInfo(string ProductId, string Title, string Price, string Description);

    public record PurchaseResult(bool Success, string ProductId, string ReceiptOrToken, string Platform);
}
