using System.Threading.Tasks;

namespace AIBookExtractor.Services
{
    public interface IAiService
    {
        Task<string> ProcessText(string text);
        void Configure(string service, string model, string apiKey, string prompt);
    }
}