import { api } from "@/src/lib/api";
import type {
  PrenotazioneConclusioneDto,
  PrenotazioneCreateDto,
  PrenotazioneDto,
  ProfiloDto,
  ProfiloUpdateDto,
  RefertoDto,
  SlotCreationBulkDto,
  SlotDto,
} from "@/src/types/dto";
import type { StatoPrenotazione } from "@/src/types/enums";

export async function fetchMioProfiloApi(): Promise<ProfiloDto> {
  return api.get<ProfiloDto>("/api/Profili/mio");
}

export async function updateMioProfiloApi(
  payload: ProfiloUpdateDto
): Promise<void> {
  await api.put<void>("/api/Profili/mio", payload);
}

export async function cercaMediciApi(params: {
  specializzazione?: string;
  nome?: string;
}): Promise<ProfiloDto[]> {
  const q = new URLSearchParams();
  if (params.specializzazione?.trim()) {
    q.set("specializzazione", params.specializzazione.trim());
  }
  if (params.nome?.trim()) {
    q.set("nome", params.nome.trim());
  }
  const qs = q.toString();
  return api.get<ProfiloDto[]>(
    `/api/Profili/medici${qs ? `?${qs}` : ""}`
  );
}

export async function fetchSlotDisponibiliApi(
  medicoId: string,
  dataInizio?: string,
  dataFine?: string
): Promise<SlotDto[]> {
  const q = new URLSearchParams();
  if (dataInizio) q.set("dataInizio", dataInizio);
  if (dataFine) q.set("dataFine", dataFine);
  const qs = q.toString();
  return api.get<SlotDto[]>(
    `/api/Slot/medici/${medicoId}/disponibili${qs ? `?${qs}` : ""}`
  );
}

export async function fetchMiaAgendaSlotApi(
  dataInizio?: string,
  dataFine?: string
): Promise<SlotDto[]> {
  const q = new URLSearchParams();
  if (dataInizio) q.set("dataInizio", dataInizio);
  if (dataFine) q.set("dataFine", dataFine);
  const qs = q.toString();
  return api.get<SlotDto[]>(`/api/Slot/mio${qs ? `?${qs}` : ""}`);
}

export async function creaSlotBulkApi(
  payload: SlotCreationBulkDto
): Promise<{ message: string; medicoId: string; slotCreati: number }> {
  return api.post("/api/Slot/bulk", payload);
}

export async function eliminaSlotApi(slotId: string): Promise<void> {
  await api.delete<void>(`/api/Slot/${slotId}`);
}

export async function creaPrenotazioneApi(
  payload: PrenotazioneCreateDto
): Promise<PrenotazioneDto> {
  return api.post<PrenotazioneDto>("/api/Prenotazioni", payload);
}

export async function fetchMieVisitePazienteApi(): Promise<PrenotazioneDto[]> {
  return api.get<PrenotazioneDto[]>("/api/Prenotazioni/paziente/mie-visite");
}

export async function fetchAgendaMedicoApi(
  dataInizio?: string,
  dataFine?: string
): Promise<PrenotazioneDto[]> {
  const q = new URLSearchParams();
  if (dataInizio) q.set("dataInizio", dataInizio);
  if (dataFine) q.set("dataFine", dataFine);
  const qs = q.toString();
  return api.get<PrenotazioneDto[]>(
    `/api/Prenotazioni/medico/agenda${qs ? `?${qs}` : ""}`
  );
}

export async function cambiaStatoPrenotazioneApi(
  id: string,
  nuovoStato: Extract<StatoPrenotazione, "confermata" | "cancellata">
): Promise<void> {
  await api.patch<void>(
    `/api/Prenotazioni/${id}/stato?nuovoStato=${encodeURIComponent(nuovoStato)}`
  );
}

export async function concludiVisitaApi(
  id: string,
  payload: PrenotazioneConclusioneDto
): Promise<void> {
  await api.post<void>(`/api/Prenotazioni/${id}/concludi`, payload);
}

export async function fetchFascicoloApi(): Promise<RefertoDto[]> {
  return api.get<RefertoDto[]>("/api/Referti/fascicolo");
}

export async function caricaRefertoApi(formData: FormData): Promise<RefertoDto> {
  return api.post<RefertoDto>("/api/Referti/carica", formData);
}

export function formatDateTime(iso: string): string {
  try {
    return new Intl.DateTimeFormat("it-IT", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function formatDate(iso: string): string {
  try {
    return new Intl.DateTimeFormat("it-IT", {
      dateStyle: "medium",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function statoLabel(stato: StatoPrenotazione): string {
  switch (stato) {
    case "in_attesa":
      return "In attesa";
    case "confermata":
      return "Confermata";
    case "completata":
      return "Completata";
    case "cancellata":
      return "Cancellata";
    default:
      return stato;
  }
}

export function buildBulkSlots(params: {
  data: string;
  oraInizio: string;
  oraFine: string;
  durataMinuti: number;
}): { dataOraInizio: string; dataOraFine: string }[] {
  const { data, oraInizio, oraFine, durataMinuti } = params;
  if (!data || !oraInizio || !oraFine || durataMinuti <= 0) return [];

  const start = new Date(`${data}T${oraInizio}:00`);
  const end = new Date(`${data}T${oraFine}:00`);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end <= start) {
    return [];
  }

  const slots: { dataOraInizio: string; dataOraFine: string }[] = [];
  const cursor = new Date(start);

  while (cursor.getTime() + durataMinuti * 60_000 <= end.getTime()) {
    const slotEnd = new Date(cursor.getTime() + durataMinuti * 60_000);
    slots.push({
      dataOraInizio: cursor.toISOString(),
      dataOraFine: slotEnd.toISOString(),
    });
    cursor.setTime(slotEnd.getTime());
  }

  return slots;
}
