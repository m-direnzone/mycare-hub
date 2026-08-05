using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Referto;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class RefertoService : IRefertoService
{
    private const int SignedUrlExpiresInSeconds = 3600;
    private const string DefaultBucketName = "referti";
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly IRefertoMedicoRepository _refertoRepository;
    private readonly IProfiloRepository _profiloRepository;
    private readonly IMedicoRepository _medicoRepository;
    private readonly IPrenotazioneRepository _prenotazioneRepository;
    private readonly Supabase.Client _supabase;
    private readonly string _bucketName;

    public RefertoService(
        IRefertoMedicoRepository refertoRepository,
        IProfiloRepository profiloRepository,
        IMedicoRepository medicoRepository,
        IPrenotazioneRepository prenotazioneRepository,
        Supabase.Client supabase,
        IConfiguration configuration)
    {
        _refertoRepository = refertoRepository;
        _profiloRepository = profiloRepository;
        _medicoRepository = medicoRepository;
        _prenotazioneRepository = prenotazioneRepository;
        _supabase = supabase;
        _bucketName = configuration["Supabase:Storage:RefertiBucket"] ?? DefaultBucketName;
    }

    public async Task<RefertoDto?> CaricaRefertoAsync(
        Guid medicoId,
        RefertoCreateDto dto,
        Stream fileStream,
        string fileName,
        string contentType)
    {
        if (fileStream is null || fileStream == Stream.Null)
            return null;

        if (!IsPdf(contentType, fileName))
            return null;

        await using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);
        if (buffer.Length == 0 || buffer.Length > MaxFileBytes)
            return null;

        var safeName = SanitizeFileName(fileName);
        var storagePath = $"{dto.PazienteId:D}/{medicoId:D}/{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}_{safeName}";
        var bytes = buffer.ToArray();

        var uploadedPath = await _supabase.Storage
            .From(_bucketName)
            .Upload(bytes, storagePath, new Supabase.Storage.FileOptions
            {
                ContentType = "application/pdf",
                Upsert = false
            });

        if (string.IsNullOrWhiteSpace(uploadedPath))
            return null;

        // Upload può restituire "bucket/path" o solo il path relativo
        var normalizedPath = uploadedPath.StartsWith($"{_bucketName}/", StringComparison.Ordinal)
            ? uploadedPath[(_bucketName.Length + 1)..]
            : uploadedPath.TrimStart('/');

        var referto = await SalvaMetaRefertoAsync(medicoId, dto, normalizedPath);
        if (referto is null)
        {
            try
            {
                await _supabase.Storage.From(_bucketName).Remove(new List<string> { normalizedPath });
            }
            catch
            {
                // Compensazione best-effort: i metadati non sono stati salvati
            }
        }

        return referto;
    }

    public async Task<RefertoDto?> SalvaMetaRefertoAsync(
        Guid medicoId,
        RefertoCreateDto dto,
        string storageFilePath)
    {
        if (string.IsNullOrWhiteSpace(storageFilePath))
            return null;

        var medicoTask = _profiloRepository.GetByIdAsync(medicoId);
        var pazienteTask = _profiloRepository.GetByIdAsync(dto.PazienteId);
        await Task.WhenAll(medicoTask, pazienteTask);

        var medico = await medicoTask;
        if (medico is null || medico.Ruolo != RuoloUtente.Medico)
            return null;

        var paziente = await pazienteTask;
        if (paziente is null || paziente.Ruolo != RuoloUtente.Paziente)
            return null;

        if (dto.PrenotazioneId.HasValue)
        {
            var prenotazione = await _prenotazioneRepository.GetByIdAsync(dto.PrenotazioneId.Value);
            if (prenotazione is null
                || prenotazione.MedicoId != medicoId
                || prenotazione.PazienteId != dto.PazienteId)
            {
                return null;
            }
        }

        var referto = new RefertoMedico
        {
            Id = Guid.NewGuid(),
            PrenotazioneId = dto.PrenotazioneId,
            PazienteId = dto.PazienteId,
            MedicoId = medicoId,
            Titolo = dto.Titolo.Trim(),
            Descrizione = string.IsNullOrWhiteSpace(dto.Descrizione) ? null : dto.Descrizione.Trim(),
            FilePath = storageFilePath.Trim(),
            DataEmissione = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatoAt = DateTimeOffset.UtcNow
        };

        var creato = await _refertoRepository.CreateAsync(referto);
        if (creato is null)
            return null;

        var dettagliMedicoTask = _medicoRepository.GetDettagliByIdAsync(medicoId);
        var downloadUrlTask = CreateSignedDownloadUrlAsync(creato.FilePath);
        await Task.WhenAll(dettagliMedicoTask, downloadUrlTask);

        return MapReferto(creato, medico, await dettagliMedicoTask, await downloadUrlTask);
    }

    public async Task<IEnumerable<RefertoDto>> GetFascicoloSanitarioPazienteAsync(Guid pazienteId)
    {
        var referti = (await _refertoRepository.GetByPazienteIdAsync(pazienteId)).ToList();
        if (referti.Count == 0)
            return [];

        var medicoIds = referti.Select(r => r.MedicoId).Distinct().ToList();
        var profiliTask = _profiloRepository.GetByIdsAsync(medicoIds);
        var dettagliTask = _medicoRepository.GetDettagliByIdsAsync(medicoIds);
        await Task.WhenAll(profiliTask, dettagliTask);

        var profili = (await profiliTask).ToDictionary(p => p.Id);
        var dettagli = (await dettagliTask).ToDictionary(d => d.MedicoId);

        var mapped = await Task.WhenAll(referti.Select(async referto =>
        {
            if (!profili.TryGetValue(referto.MedicoId, out var medico))
                return null;

            dettagli.TryGetValue(referto.MedicoId, out var dettagliMedico);
            var downloadUrl = await CreateSignedDownloadUrlAsync(referto.FilePath);
            return MapReferto(referto, medico, dettagliMedico, downloadUrl);
        }));

        return mapped.Where(dto => dto is not null).Cast<RefertoDto>().ToList();
    }

    public async Task<RefertoDto?> GetRefertoByIdAsync(Guid refertoId)
    {
        var referto = await _refertoRepository.GetByIdAsync(refertoId);
        if (referto is null)
            return null;

        return await MapRefertoConSignedUrlAsync(referto);
    }

    private async Task<RefertoDto?> MapRefertoConSignedUrlAsync(RefertoMedico referto)
    {
        var medicoTask = _profiloRepository.GetByIdAsync(referto.MedicoId);
        var dettagliTask = _medicoRepository.GetDettagliByIdAsync(referto.MedicoId);
        var downloadUrlTask = CreateSignedDownloadUrlAsync(referto.FilePath);
        await Task.WhenAll(medicoTask, dettagliTask, downloadUrlTask);

        var medico = await medicoTask;
        if (medico is null)
            return null;

        return MapReferto(referto, medico, await dettagliTask, await downloadUrlTask);
    }

    private async Task<string> CreateSignedDownloadUrlAsync(string filePath)
    {
        // Signed URL a tempo (1 ora): nessun URL pubblico permanente sul PDF clinico
        var signedUrl = await _supabase.Storage
            .From(_bucketName)
            .CreateSignedUrl(filePath, SignedUrlExpiresInSeconds);

        return signedUrl;
    }

    private static RefertoDto MapReferto(
        RefertoMedico referto,
        Profilo medico,
        MedicoDettaglio? dettagliMedico,
        string downloadUrl) => new()
    {
        Id = referto.Id,
        PrenotazioneId = referto.PrenotazioneId,
        PazienteId = referto.PazienteId,
        MedicoId = referto.MedicoId,
        Titolo = referto.Titolo,
        Descrizione = referto.Descrizione,
        DataEmissione = referto.DataEmissione,
        DownloadUrl = downloadUrl,
        MedicoNomeCompleto = $"{medico.Nome} {medico.Cognome}",
        MedicoSpecializzazione = dettagliMedico?.Specializzazione ?? string.Empty
    };

    private static bool IsPdf(string contentType, string fileName)
    {
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return true;

        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(name))
            return "referto.pdf";

        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            name += ".pdf";

        return name.Length > 120 ? name[^120..] : name;
    }
}
