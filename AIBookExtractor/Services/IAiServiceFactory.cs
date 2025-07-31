using System;
using AIBookExtractor.Services.Configuration;
using AIBookExtractor.Services.Implementations;

namespace AIBookExtractor.Services
{
    public interface IAiServiceFactory
    {
        IAiService CreateService(string serviceName);
    }

    public class AiServiceFactory : IAiServiceFactory
    {
        private readonly IAiServiceConfiguration _configuration;

        public AiServiceFactory(IAiServiceConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IAiService CreateService(string serviceName)
        {
            var services = _configuration.GetAvailableServices();
            var serviceDefinition = services.Find(s => s.Name == serviceName);
            
            if (serviceDefinition == null)
            {
                throw new ArgumentException($"Unknown service: {serviceName}");
            }

            return serviceDefinition.ServiceType switch
            {
                AiServiceType.OpenAI => new OpenAiService(),
                AiServiceType.Anthropic => new AnthropicService(),
                AiServiceType.AzureOpenAI => new AzureOpenAiService(),
                AiServiceType.Local => new LocalLlmService(),
                _ => throw new NotImplementedException($"Service type {serviceDefinition.ServiceType} not implemented")
            };
        }
    }
}