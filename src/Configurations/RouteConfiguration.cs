namespace ApiGateway.Configurations;

public class ServicesConfiguration
{
    public ServiceEndpoint UserService { get; set; } = new();
    public ServiceEndpoint AuthService { get; set; } = new();
    public ServiceEndpoint VideoProcessingService { get; set; } = new();
    public ServiceEndpoint NotificationService { get; set; } = new();
}

public class ServiceEndpoint
{
    public string Url { get; set; } = string.Empty;

    public void Validate(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(Url))
            throw new InvalidOperationException($"{serviceName} URL é obrigatória");
        
        if (!Uri.TryCreate(Url, UriKind.Absolute, out _))
            throw new InvalidOperationException($"{serviceName} URL é inválida: {Url}");
    }
}