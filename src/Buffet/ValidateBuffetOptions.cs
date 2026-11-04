using System.Collections.Generic;
using Buffet.Core;
using Microsoft.Extensions.Options;

namespace Buffet;

/// <summary>
/// Buffet's options configuration, specific to the default Buffet application.
/// Don't use this if you are embedding Buffet into your own custom ASP.NET Core application.
/// </summary>
public class ValidateBuffetOptions
    : IValidateOptions<BuffetOptions>
{
    public ValidateOptionsResult Validate(string name, BuffetOptions options)
    {
        var failures = new List<string>();

        if (options.Database == null) failures.Add($"The '{nameof(BuffetOptions.Database)}' config is required");
        if (options.Mirror == null) failures.Add($"The '{nameof(BuffetOptions.Mirror)}' config is required");

        if (failures.Count != 0) return ValidateOptionsResult.Fail(failures);

        return ValidateOptionsResult.Success;
    }
}
