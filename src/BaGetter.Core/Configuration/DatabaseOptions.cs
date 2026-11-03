using System.ComponentModel.DataAnnotations;

namespace BaGetter.Core;

public class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; }
}
