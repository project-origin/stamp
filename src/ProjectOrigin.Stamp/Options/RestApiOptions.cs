using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ProjectOrigin.Stamp.Options;

public class RestApiOptions
{
    [Required(AllowEmptyStrings = false)]
    public PathString PathBase { get; set; } = string.Empty;
}
