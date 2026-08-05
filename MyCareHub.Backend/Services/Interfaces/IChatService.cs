using MyCareHub.Backend.Models.DTOs.Chat;

namespace MyCareHub.Backend.Services.Interfaces;

public interface IChatService
{
    Task<IEnumerable<ConversazioneDto>> GetMieConversazioniAsync(Guid utenteId, bool isMedico);

    Task<IEnumerable<ChatContattoDto>> GetContattiAsync(Guid utenteId, bool isMedico);

    Task<ConversazioneDto?> GetOrCreateConversazioneAsync(
        Guid utenteId,
        bool isMedico,
        Guid contattoId);

    Task<ConversazioneDto?> GetConversazioneAsync(Guid conversazioneId, Guid utenteId);

    Task<IEnumerable<MessaggioDto>> GetMessaggiAsync(
        Guid conversazioneId,
        Guid utenteId,
        DateTimeOffset? after = null);

    Task<MessaggioDto?> InviaMessaggioAsync(
        Guid conversazioneId,
        Guid mittenteId,
        MessaggioCreateDto dto);

    Task<bool> SegnaMessaggiLettiAsync(Guid conversazioneId, Guid lettoreId);
}
