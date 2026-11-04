using System.ComponentModel.DataAnnotations;

namespace Buffet.Core;

public class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; }
}
