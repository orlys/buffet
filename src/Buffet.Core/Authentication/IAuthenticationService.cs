using System.Threading;
using System.Threading.Tasks;

namespace Buffet.Core;

public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync(string apiKey, CancellationToken cancellationToken);
}
