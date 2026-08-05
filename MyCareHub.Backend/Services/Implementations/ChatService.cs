using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Chat;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class ChatService : IChatService
{
    private readonly IConversazioneRepository _conversazioneRepository;
    private readonly IMessaggioRepository _messaggioRepository;
    private readonly IProfiloRepository _profiloRepository;
    private readonly IPrenotazioneRepository _prenotazioneRepository;
    private readonly IMedicoRepository _medicoRepository;

    public ChatService(
        IConversazioneRepository conversazioneRepository,
        IMessaggioRepository messaggioRepository,
        IProfiloRepository profiloRepository,
        IPrenotazioneRepository prenotazioneRepository,
        IMedicoRepository medicoRepository)
    {
        _conversazioneRepository = conversazioneRepository;
        _messaggioRepository = messaggioRepository;
        _profiloRepository = profiloRepository;
        _prenotazioneRepository = prenotazioneRepository;
        _medicoRepository = medicoRepository;
    }

    public async Task<IEnumerable<ConversazioneDto>> GetMieConversazioniAsync(Guid utenteId, bool isMedico)
    {
        var conversazioni = (isMedico
            ? await _conversazioneRepository.GetByMedicoIdAsync(utenteId)
            : await _conversazioneRepository.GetByPazienteIdAsync(utenteId)).ToList();

        if (conversazioni.Count == 0)
            return [];

        var contattoIds = conversazioni
            .Select(c => c.MedicoId == utenteId ? c.PazienteId : c.MedicoId)
            .Distinct()
            .ToList();

        var profiliTask = _profiloRepository.GetByIdsAsync(contattoIds);
        var dettagliTask = isMedico
            ? Task.FromResult(Enumerable.Empty<MedicoDettaglio>())
            : _medicoRepository.GetDettagliByIdsAsync(
                conversazioni.Select(c => c.MedicoId).Distinct());

        await Task.WhenAll(profiliTask, dettagliTask);

        var profili = (await profiliTask).ToDictionary(p => p.Id);
        var dettagli = (await dettagliTask).ToDictionary(d => d.MedicoId);

        var mapped = await Task.WhenAll(conversazioni.Select(async c =>
        {
            var isViewerMedico = c.MedicoId == utenteId;
            var contattoId = isViewerMedico ? c.PazienteId : c.MedicoId;
            profili.TryGetValue(contattoId, out var contatto);

            string? sottoTitolo = null;
            if (!isViewerMedico)
            {
                dettagli.TryGetValue(c.MedicoId, out var dett);
                sottoTitolo = dett?.Specializzazione;
            }
            else if (contatto is not null)
            {
                sottoTitolo = contatto.CodiceFiscale;
            }

            var ultimoTask = _messaggioRepository.GetUltimoByConversazioneIdAsync(c.Id);
            var nonLettiTask = _messaggioRepository.CountNonLettiAsync(c.Id, utenteId);
            await Task.WhenAll(ultimoTask, nonLettiTask);

            var ultimo = await ultimoTask;
            return new ConversazioneDto
            {
                Id = c.Id,
                PazienteId = c.PazienteId,
                MedicoId = c.MedicoId,
                ContattoNomeCompleto = contatto is null
                    ? "Utente"
                    : $"{contatto.Nome} {contatto.Cognome}".Trim(),
                ContattoSottoTitolo = sottoTitolo,
                CreatoAt = c.CreatoAt,
                AggiornatoAt = c.AggiornatoAt,
                UltimoMessaggioAt = c.UltimoMessaggioAt ?? ultimo?.CreatoAt,
                AnteprimaUltimoMessaggio = Truncate(ultimo?.Contenuto, 80),
                MessaggiNonLetti = await nonLettiTask
            };
        }));

        return mapped;
    }

    public async Task<IEnumerable<ChatContattoDto>> GetContattiAsync(Guid utenteId, bool isMedico)
    {
        if (isMedico)
        {
            var prenotazioniTask = _prenotazioneRepository.GetByMedicoIdAsync(utenteId);
            var conversazioniTask = _conversazioneRepository.GetByMedicoIdAsync(utenteId);
            await Task.WhenAll(prenotazioniTask, conversazioniTask);

            var pazienteIds = (await prenotazioniTask)
                .Select(p => p.PazienteId)
                .Distinct()
                .ToList();

            var profili = (await _profiloRepository.GetByIdsAsync(pazienteIds))
                .Where(p => p.Ruolo == RuoloUtente.Paziente)
                .ToDictionary(p => p.Id);

            var conversazioni = (await conversazioniTask)
                .ToDictionary(c => c.PazienteId);

            return pazienteIds
                .Where(id => profili.ContainsKey(id))
                .Select(id =>
                {
                    var p = profili[id];
                    conversazioni.TryGetValue(id, out var conv);
                    return new ChatContattoDto
                    {
                        Id = id,
                        NomeCompleto = $"{p.Nome} {p.Cognome}".Trim(),
                        SottoTitolo = p.CodiceFiscale,
                        ConversazioneId = conv?.Id
                    };
                })
                .OrderBy(c => c.NomeCompleto)
                .ToList();
        }
        else
        {
            var prenotazioniTask = _prenotazioneRepository.GetByPazienteIdAsync(utenteId);
            var conversazioniTask = _conversazioneRepository.GetByPazienteIdAsync(utenteId);
            await Task.WhenAll(prenotazioniTask, conversazioniTask);

            var medicoIds = (await prenotazioniTask)
                .Select(p => p.MedicoId)
                .Distinct()
                .ToList();

            var profiliTask = _profiloRepository.GetByIdsAsync(medicoIds);
            var dettagliTask = _medicoRepository.GetDettagliByIdsAsync(medicoIds);
            await Task.WhenAll(profiliTask, dettagliTask);

            var profili = (await profiliTask)
                .Where(p => p.Ruolo == RuoloUtente.Medico)
                .ToDictionary(p => p.Id);
            var dettagli = (await dettagliTask).ToDictionary(d => d.MedicoId);

            var conversazioni = (await conversazioniTask)
                .ToDictionary(c => c.MedicoId);

            return medicoIds
                .Where(id => profili.ContainsKey(id))
                .Select(id =>
                {
                    var p = profili[id];
                    dettagli.TryGetValue(id, out var dett);
                    conversazioni.TryGetValue(id, out var conv);
                    return new ChatContattoDto
                    {
                        Id = id,
                        NomeCompleto = $"{p.Nome} {p.Cognome}".Trim(),
                        SottoTitolo = dett?.Specializzazione,
                        ConversazioneId = conv?.Id
                    };
                })
                .OrderBy(c => c.NomeCompleto)
                .ToList();
        }
    }

    public async Task<ConversazioneDto?> GetOrCreateConversazioneAsync(
        Guid utenteId,
        bool isMedico,
        Guid contattoId)
    {
        if (contattoId == utenteId)
            return null;

        Guid pazienteId;
        Guid medicoId;

        if (isMedico)
        {
            var paziente = await _profiloRepository.GetByIdAsync(contattoId);
            if (paziente is null || paziente.Ruolo != RuoloUtente.Paziente)
                return null;

            medicoId = utenteId;
            pazienteId = contattoId;
        }
        else
        {
            var medico = await _profiloRepository.GetByIdAsync(contattoId);
            if (medico is null || medico.Ruolo != RuoloUtente.Medico)
                return null;

            pazienteId = utenteId;
            medicoId = contattoId;
        }

        if (!await HannoPrenotazioneInComuneAsync(pazienteId, medicoId))
            return null;

        var esistente = await _conversazioneRepository.GetByPartecipantiAsync(pazienteId, medicoId);
        if (esistente is not null)
            return await MapConversazioneAsync(esistente, utenteId);

        var now = DateTimeOffset.UtcNow;
        var creata = await _conversazioneRepository.CreateAsync(new Conversazione
        {
            Id = Guid.NewGuid(),
            PazienteId = pazienteId,
            MedicoId = medicoId,
            CreatoAt = now,
            AggiornatoAt = now
        });

        if (creata is null)
            return null;

        return await MapConversazioneAsync(creata, utenteId);
    }

    public async Task<ConversazioneDto?> GetConversazioneAsync(Guid conversazioneId, Guid utenteId)
    {
        var conversazione = await _conversazioneRepository.GetByIdAsync(conversazioneId);
        if (conversazione is null)
            return null;

        if (!IsPartecipante(conversazione, utenteId))
            return null;

        return await MapConversazioneAsync(conversazione, utenteId);
    }

    public async Task<IEnumerable<MessaggioDto>> GetMessaggiAsync(
        Guid conversazioneId,
        Guid utenteId,
        DateTimeOffset? after = null)
    {
        var conversazione = await _conversazioneRepository.GetByIdAsync(conversazioneId);
        if (conversazione is null || !IsPartecipante(conversazione, utenteId))
            return [];

        var messaggi = await _messaggioRepository.GetByConversazioneIdAsync(conversazioneId, after);
        return messaggi.Select(m => MapMessaggio(m, utenteId)).ToList();
    }

    public async Task<MessaggioDto?> InviaMessaggioAsync(
        Guid conversazioneId,
        Guid mittenteId,
        MessaggioCreateDto dto)
    {
        var contenuto = dto.Contenuto?.Trim();
        if (string.IsNullOrWhiteSpace(contenuto) || contenuto.Length > 4000)
            return null;

        var conversazione = await _conversazioneRepository.GetByIdAsync(conversazioneId);
        if (conversazione is null || !IsPartecipante(conversazione, mittenteId))
            return null;

        var now = DateTimeOffset.UtcNow;
        var creato = await _messaggioRepository.CreateAsync(new Messaggio
        {
            Id = Guid.NewGuid(),
            ConversazioneId = conversazioneId,
            MittenteId = mittenteId,
            Contenuto = contenuto,
            CreatoAt = now
        });

        if (creato is null)
            return null;

        // Best-effort: il trigger DB aggiorna già ultimo_messaggio_at.
        await _conversazioneRepository.TouchUltimoMessaggioAsync(conversazioneId, creato.CreatoAt);

        return MapMessaggio(creato, mittenteId);
    }

    public async Task<bool> SegnaMessaggiLettiAsync(Guid conversazioneId, Guid lettoreId)
    {
        var conversazione = await _conversazioneRepository.GetByIdAsync(conversazioneId);
        if (conversazione is null || !IsPartecipante(conversazione, lettoreId))
            return false;

        return await _messaggioRepository.SegnaLettiAsync(conversazioneId, lettoreId, DateTimeOffset.UtcNow);
    }

    private async Task<bool> HannoPrenotazioneInComuneAsync(Guid pazienteId, Guid medicoId)
    {
        var prenotazioni = await _prenotazioneRepository.GetByPazienteIdAsync(pazienteId);
        return prenotazioni.Any(p =>
            p.MedicoId == medicoId &&
            p.Stato != StatoPrenotazione.Cancellata);
    }

    private static bool IsPartecipante(Conversazione conversazione, Guid utenteId) =>
        conversazione.PazienteId == utenteId || conversazione.MedicoId == utenteId;

    private async Task<ConversazioneDto> MapConversazioneAsync(Conversazione c, Guid utenteId)
    {
        var isMedico = c.MedicoId == utenteId;
        var contattoId = isMedico ? c.PazienteId : c.MedicoId;
        var contattoTask = _profiloRepository.GetByIdAsync(contattoId);
        var dettagliTask = isMedico
            ? Task.FromResult<MedicoDettaglio?>(null)
            : _medicoRepository.GetDettagliByIdAsync(c.MedicoId);
        var ultimoTask = _messaggioRepository.GetUltimoByConversazioneIdAsync(c.Id);
        var nonLettiTask = _messaggioRepository.CountNonLettiAsync(c.Id, utenteId);

        await Task.WhenAll(contattoTask, dettagliTask, ultimoTask, nonLettiTask);

        var contatto = await contattoTask;
        string? sottoTitolo = null;
        if (!isMedico)
            sottoTitolo = (await dettagliTask)?.Specializzazione;
        else if (contatto is not null)
            sottoTitolo = contatto.CodiceFiscale;

        var ultimo = await ultimoTask;

        return new ConversazioneDto
        {
            Id = c.Id,
            PazienteId = c.PazienteId,
            MedicoId = c.MedicoId,
            ContattoNomeCompleto = contatto is null
                ? "Utente"
                : $"{contatto.Nome} {contatto.Cognome}".Trim(),
            ContattoSottoTitolo = sottoTitolo,
            CreatoAt = c.CreatoAt,
            AggiornatoAt = c.AggiornatoAt,
            UltimoMessaggioAt = c.UltimoMessaggioAt ?? ultimo?.CreatoAt,
            AnteprimaUltimoMessaggio = Truncate(ultimo?.Contenuto, 80),
            MessaggiNonLetti = await nonLettiTask
        };
    }

    private static MessaggioDto MapMessaggio(Messaggio m, Guid utenteId) => new()
    {
        Id = m.Id,
        ConversazioneId = m.ConversazioneId,
        MittenteId = m.MittenteId,
        Contenuto = m.Contenuto,
        CreatoAt = m.CreatoAt,
        LettoAt = m.LettoAt,
        IsMio = m.MittenteId == utenteId
    };

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }
}
