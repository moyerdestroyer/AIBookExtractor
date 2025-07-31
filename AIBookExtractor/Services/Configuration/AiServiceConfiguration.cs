using System.Collections.Generic;
using System.Linq;

namespace AIBookExtractor.Services.Configuration
{
    public class AiServiceConfiguration : IAiServiceConfiguration
    {
        private readonly List<AiServiceDefinition> _services;

        public AiServiceConfiguration()
        {
            _services = InitializeServices();
        }

        public List<AiServiceDefinition> GetAvailableServices()
        {
            return _services.ToList(); // Return a copy
        }

        public List<string> GetModelsForService(string serviceName)
        {
            var service = _services.FirstOrDefault(s => s.Name == serviceName);
            return service?.Models.Select(m => m.Name).ToList() ?? new List<string>();
        }

        private List<AiServiceDefinition> InitializeServices()
        {
            return new List<AiServiceDefinition>
            {
                new AiServiceDefinition
                {
                    Name = "openai",
                    DisplayName = "OpenAI",
                    ServiceType = AiServiceType.OpenAI,
                    RequiresApiKey = true,
                    ApiKeyHelpText = "Get your API key from https://platform.openai.com/api-keys",
                    Models = new List<AiModelDefinition>
                    {
                        new AiModelDefinition
                        {
                            Name = "gpt-4-turbo-preview",
                            DisplayName = "GPT-4 Turbo",
                            MaxTokens = 128000,
                            CostPer1kTokens = 0.01m,
                            Description = "Most capable model, best for complex tasks"
                        },
                        new AiModelDefinition
                        {
                            Name = "gpt-4",
                            DisplayName = "GPT-4",
                            MaxTokens = 8192,
                            CostPer1kTokens = 0.03m,
                            Description = "High capability, more expensive"
                        },
                        new AiModelDefinition
                        {
                            Name = "gpt-3.5-turbo",
                            DisplayName = "GPT-3.5 Turbo",
                            MaxTokens = 16385,
                            CostPer1kTokens = 0.001m,
                            Description = "Fast and cost-effective"
                        }
                    }
                },
                new AiServiceDefinition
                {
                    Name = "anthropic",
                    DisplayName = "Anthropic Claude",
                    ServiceType = AiServiceType.Anthropic,
                    RequiresApiKey = true,
                    ApiKeyHelpText = "Get your API key from https://console.anthropic.com/",
                    Models = new List<AiModelDefinition>
                    {
                        new AiModelDefinition
                        {
                            Name = "claude-3-opus-20240229",
                            DisplayName = "Claude 3 Opus",
                            MaxTokens = 200000,
                            CostPer1kTokens = 0.015m,
                            Description = "Most intelligent Claude model"
                        },
                        new AiModelDefinition
                        {
                            Name = "claude-3-sonnet-20240229",
                            DisplayName = "Claude 3 Sonnet",
                            MaxTokens = 200000,
                            CostPer1kTokens = 0.003m,
                            Description = "Balanced performance and cost"
                        },
                        new AiModelDefinition
                        {
                            Name = "claude-3-haiku-20240307",
                            DisplayName = "Claude 3 Haiku",
                            MaxTokens = 200000,
                            CostPer1kTokens = 0.00025m,
                            Description = "Fastest and most affordable"
                        }
                    }
                },
                new AiServiceDefinition
                {
                    Name = "azure",
                    DisplayName = "Azure OpenAI",
                    ServiceType = AiServiceType.AzureOpenAI,
                    RequiresApiKey = true,
                    ApiKeyHelpText = "Configure in Azure Portal",
                    Models = new List<AiModelDefinition>
                    {
                        new AiModelDefinition
                        {
                            Name = "gpt-4",
                            DisplayName = "GPT-4 (Azure)",
                            MaxTokens = 8192,
                            Description = "Deployed via Azure"
                        },
                        new AiModelDefinition
                        {
                            Name = "gpt-35-turbo",
                            DisplayName = "GPT-3.5 Turbo (Azure)",
                            MaxTokens = 4096,
                            Description = "Deployed via Azure"
                        }
                    }
                },
                new AiServiceDefinition
                {
                    Name = "local",
                    DisplayName = "Local LLM (Ollama)",
                    ServiceType = AiServiceType.Local,
                    RequiresApiKey = false,
                    Models = new List<AiModelDefinition>
                    {
                        new AiModelDefinition
                        {
                            Name = "llama2",
                            DisplayName = "Llama 2",
                            MaxTokens = 4096,
                            Description = "Meta's Llama 2 model"
                        },
                        new AiModelDefinition
                        {
                            Name = "mistral",
                            DisplayName = "Mistral 7B",
                            MaxTokens = 8192,
                            Description = "Efficient open model"
                        },
                        new AiModelDefinition
                        {
                            Name = "mixtral",
                            DisplayName = "Mixtral 8x7B",
                            MaxTokens = 32768,
                            Description = "MoE model with high capability"
                        }
                    }
                }
            };
        }
    }
}