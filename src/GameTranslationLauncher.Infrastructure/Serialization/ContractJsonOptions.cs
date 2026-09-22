using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameTranslationLauncher.Infrastructure.Serialization;

internal static class ContractJsonOptions
{
    public static JsonSerializerOptions Create(bool writeIndented = false)
    {
        return new JsonSerializerOptions
        {
            AllowDuplicateProperties = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = writeIndented
        };
    }
}
