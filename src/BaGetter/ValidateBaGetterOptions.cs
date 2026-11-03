using System.Collections.Generic;
using BaGetter.Core;
using Microsoft.Extensions.Options;

namespace BaGetter;

/// <summary>
/// BaGetter's options configuration, specific to the default BaGetter application.
/// Don't use this if you are embedding BaGetter into your own custom ASP.NET Core application.
/// </summary>
public class ValidateBaGetterOptions
    : IValidateOptions<BaGetterOptions>
{
    public ValidateOptionsResult Validate(string name, BaGetterOptions options)
    {
        var failures = new List<string>();

        if (options.Database == null) failures.Add($"The '{nameof(BaGetterOptions.Database)}' config is required");
        if (options.Mirror == null) failures.Add($"The '{nameof(BaGetterOptions.Mirror)}' config is required");

        if (failures.Count != 0) return ValidateOptionsResult.Fail(failures);

        return ValidateOptionsResult.Success;
    }
}
