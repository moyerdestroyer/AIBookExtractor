using System.Collections.Generic;

namespace AIBookExtractor.Services.Configuration
{
    public interface IAiServiceConfiguration
    {
        List<AiServiceDefinition> GetAvailableServices();
        List<string> GetModelsForService(string serviceName);
    }

    public class AiServiceDefinition
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public AiServiceType ServiceType { get; set; }
        public List<AiModelDefinition> Models { get; set; } = new List<AiModelDefinition>();
        public bool RequiresApiKey { get; set; } = true;
        public string? ApiKeyHelpText { get; set; }
    }

    public class AiModelDefinition
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int MaxTokens { get; set; }
        public decimal? CostPer1kTokens { get; set; }
        public string? Description { get; set; }
    }

    public enum AiServiceType
    {
        OpenAI,
        Anthropic,
        GoogleVertex,
        AzureOpenAI,
        Local,
        Custom
    }
}