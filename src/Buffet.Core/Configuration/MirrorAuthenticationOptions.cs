using System.Collections.Generic;

namespace Buffet.Core;

public class MirrorAuthenticationOptions
{
    public MirrorAuthenticationType Type { get; set; } = MirrorAuthenticationType.None;

    public string Username { get; set; }

    public string Password { get; set; }

    public string Token { get; set; }

    public Dictionary<string, string> CustomHeaders { get; set; } = [];
}
