using System.Text.Json.Serialization;

namespace MyCareHub.Backend.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatoPrenotazione
{
    [JsonStringEnumMemberName("in_attesa")]
    InAttesa,

    [JsonStringEnumMemberName("confermata")]
    Confermata,

    [JsonStringEnumMemberName("completata")]
    Completata,

    [JsonStringEnumMemberName("cancellata")]
    Cancellata
}
