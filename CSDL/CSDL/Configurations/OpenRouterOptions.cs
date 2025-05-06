namespace CSDL.Configurations
{
    public class OpenRouterOptions
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public string Model { get; set; } = "openai/gpt-3.5-turbo"; // hoặc model khác nếu bạn muốn
    }
}
