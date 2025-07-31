namespace AIBookExtractor.models {
    internal class Settings {
        //Service selector, model, api key, prompt text
        public string? Service { get; set; }
        public string? Model { get; set; }
        public string? ApiKey { get; set; }
        public string? PromptText { get; set; }
    }
}