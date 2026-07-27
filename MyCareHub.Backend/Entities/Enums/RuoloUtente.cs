using System.Text.Json.Serialization;

namespace MyCareHub.Backend.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuoloUtente
{
    [JsonStringEnumMemberName("paziente")]
    Paziente,

    [JsonStringEnumMemberName("medico")]
    Medico,

    [JsonStringEnumMemberName("admin")]
    Admin
}
