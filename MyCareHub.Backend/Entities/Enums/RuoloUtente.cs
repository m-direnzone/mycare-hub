using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MyCareHub.Backend.Entities.Enums;

[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum RuoloUtente
{
    [EnumMember(Value = "paziente")]
    [JsonStringEnumMemberName("paziente")]
    Paziente,

    [EnumMember(Value = "medico")]
    [JsonStringEnumMemberName("medico")]
    Medico,

    [EnumMember(Value = "admin")]
    [JsonStringEnumMemberName("admin")]
    Admin
}
