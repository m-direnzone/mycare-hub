using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MyCareHub.Backend.Entities.Enums;

[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
[Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
public enum StatoPrenotazione
{
    [EnumMember(Value = "in_attesa")]
    [JsonStringEnumMemberName("in_attesa")]
    InAttesa,

    [EnumMember(Value = "confermata")]
    [JsonStringEnumMemberName("confermata")]
    Confermata,

    [EnumMember(Value = "completata")]
    [JsonStringEnumMemberName("completata")]
    Completata,

    [EnumMember(Value = "cancellata")]
    [JsonStringEnumMemberName("cancellata")]
    Cancellata
}
