namespace DMIProxy.DomainService;

public interface INtfyService
{
    Task<bool> SendNotification(string message);
}
