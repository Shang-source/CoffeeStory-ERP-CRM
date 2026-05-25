using Microsoft.Extensions.Options;

namespace StoryCoffee.Infrastructure.Services;

public sealed class PortalLinkProvider(IOptions<PortalOptions> options) : IPortalLinkProvider
{
    public string LoginUrl => options.Value.BaseUrl.TrimEnd('/');
}
