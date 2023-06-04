using Domain.Services;

namespace Wasm.Services;

public class UrlHelperService : IUrlHelperService
{
    
    public string SignalUrl { get; }

    public UrlHelperService(string signalRUrl)
    {
        SignalUrl = signalRUrl;
    }

}