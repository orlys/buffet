using System.Collections.Generic;
using Buffet.Protocol.Models;

namespace Buffet.Core;

public interface ISearchResponseBuilder
{
    SearchResponse BuildSearch(IReadOnlyList<PackageRegistration> results);
    AutocompleteResponse BuildAutocomplete(IReadOnlyList<string> data);
    DependentsResponse BuildDependents(IReadOnlyList<PackageDependent> results);
}
