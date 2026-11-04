using System.Text.Json.Serialization;
using Buffet.Protocol.Models;

namespace Buffet.Core;

/// <summary>
/// Buffet's extensions to a registration index page.
/// </summary>
/// <remarks>Extends <see cref="RegistrationIndexPageItem"/>.</remarks>
public class BuffetRegistrationIndexPageItem : RegistrationIndexPageItem
{
    /// <summary>
    /// The catalog entry containing the package metadata.
    /// </summary>
    /// <remarks>This was modified to use Buffet's extended package metadata model.</remarks>
    [JsonPropertyName("catalogEntry")]
    [JsonPropertyOrder(int.MaxValue)]
    public new BuffetPackageMetadata PackageMetadata { get; set; }
}
