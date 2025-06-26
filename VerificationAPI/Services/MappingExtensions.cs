using VerificationAPI.DTO;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Services
{
    public static class MappingExtensions
    {
        public static Video ToXmlModel(this APIResponse dto) => new()
        {
            Id = dto.Id,
            Url = dto.Url,
            Author = dto.Author,
            Title = dto.Title,
            Duration = dto.Duration,
            Medias = dto.Medias
                .Select(m => new Media
                {
                    Url = m.Url,
                    DataSize = m.DataSize,
                    Quality = m.Quality,
                    Extension = m.Extension,
                    Type = m.Type
                })
                .ToList()
        };
    }

}
