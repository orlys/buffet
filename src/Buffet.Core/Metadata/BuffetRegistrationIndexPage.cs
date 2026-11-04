using System.Collections.Generic;
using System.Text.Json.Serialization;
using Buffet.Protocol.Models;

namespace Buffet.Core;

/// <summary>
/// Buffet's extensions to a registration index page.
/// </summary>
/// <remarks>Extends <see cref="RegistrationIndexPage"/>.</remarks>
public class BuffetRegistrationIndexPage : RegistrationIndexPage
{
    /// <summary>
    /// <see langword="null"/> if this package's registration is paged. The items can be found
    /// by following the page's <see cref="RegistrationIndexPage.RegistrationPageUrl"/>.
    /// </summary>
    /// <remarks>This was modified to use Buffet's extended registration index page item model.</remarks>
    [JsonPropertyName("items")]
    [JsonPropertyOrder(int.MaxValue)]
    public new IReadOnlyList<BuffetRegistrationIndexPageItem> ItemsOrNull { get; set; }
}
