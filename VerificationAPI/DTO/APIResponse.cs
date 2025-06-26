using static VerificationAPI.DTO.APIResponse;
using System.Text.Json.Serialization;

namespace VerificationAPI.DTO
{
    public record APIResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("duration")] long Duration,
    [property: JsonPropertyName("medias")] List<Media> Medias)
    {
        public record Media(
            [property: JsonPropertyName("url")] string Url,
            [property: JsonPropertyName("data_size")] long DataSize,
            [property: JsonPropertyName("quality")] string Quality,
            [property: JsonPropertyName("extension")] string Extension,
            [property: JsonPropertyName("type")] string Type);
    }
}
