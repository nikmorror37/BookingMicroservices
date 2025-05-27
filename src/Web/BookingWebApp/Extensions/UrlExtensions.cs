namespace BookingWebApp.Extensions;
public static class UrlExtensions
{
    // doing "/images/abc.jpg" --> "http://apigateway/api/images/abc.jpg"
    public static string ToGatewayUrl(this string? relative, string gatewayBase)
        => string.IsNullOrWhiteSpace(relative)
           ? string.Empty
           : (relative.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? relative
                : $"{gatewayBase.TrimEnd('/')}{relative}");
}