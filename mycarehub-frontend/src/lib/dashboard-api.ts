import { api } from "@/src/lib/api";
import { updateMioProfiloFromDb } from "@/src/lib/profilo";
import { supabase } from "@/src/lib/supabase/client";
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

type MedicoDettaglioRow = {
  medico_id: string;
  specializzazione: string;
  numero_iscrizione_albo: string;
  biografia: string | null;
  tariffa_visita: number;
  attivo: boolean;
  profili: {
    id: string;
    nome: string;
    cognome: string;
    codice_fiscale: string;
    data_nascita: string;
    telefono: string | null;
    ruolo: "medico";
  } | null;
};

async function cercaMediciFromDb(params: {
  specializzazione?: string;
  nome?: string;
}): Promise<ProfiloDto[]> {
  let query = supabase
    .from("medici_dettagli")
    .select(
      "medico_id, specializzazione, numero_iscrizione_albo, biografia, tariffa_visita, attivo, profili!inner(id, nome, cognome, codice_fiscale, data_nascita, telefono, ruolo)"
    )
    .eq("attivo", true);

  if (params.specializzazione?.trim()) {
    query = query.ilike(
      "specializzazione",
      `%${params.specializzazione.trim()}%`
    );
  }

  const { data, error } = await query;
  if (error) {
    throw new Error(error.message);
  }

  const rows = (data ?? []) as MedicoDettaglioRow[];
  const mapped = rows
    .filter((row) => row.profili?.ruolo === "medico")
    .map((row) => {
      const profilo = row.profili!;
      return {
        id: profilo.id,
        nome: profilo.nome,
        cognome: profilo.cognome,
        codiceFiscale: profilo.codice_fiscale,
        dataNascita: profilo.data_nascita,
        telefono: profilo.telefono,
        ruolo: "medico" as const,
        dettagliMedico: {
          specializzazione: row.specializzazione,
          numeroIscrizioneAlbo: row.numero_iscrizione_albo,
          biografia: row.biografia,
          tariffaVisita: Number(row.tariffa_visita),
          attivo: row.attivo,
        },
      } satisfies ProfiloDto;
    });

  const nome = params.nome?.trim();
  if (!nome) return mapped;

  const needle = nome.toLowerCase();
  return mapped.filter((m) => {
    const full = `${m.nome} ${m.cognome}`.toLowerCase();
    return (
      full.includes(needle) ||
      m.nome.toLowerCase().includes(needle) ||
      m.cognome.toLowerCase().includes(needle)
    );
  });
}

export async function fetchMioProfiloApi(): Promise<ProfiloDto> {
  return api.get<ProfiloDto>("/api/Profili/mio");
}

export async function updateMioProfiloApi(
  payload: ProfiloUpdateDto
): Promise<ProfiloDto> {
  // Via sessione Supabase (RLS profili_update_own): l'API backend con sole
  // publishable key non ha auth.uid() e l'UPDATE su profili fallisce.
  return updateMioProfiloFromDb(payload);
}

export async function cercaMediciApi(params: {
  specializzazione?: string;
  nome?: string;
}): Promise<ProfiloDto[]> {
  try {
    const q = new URLSearchParams();
    if (params.specializzazione?.trim()) {
      q.set("specializzazione", params.specializzazione.trim());
    }
    if (params.nome?.trim()) {
      q.set("nome", params.nome.trim());
    }
    const qs = q.toString();
    const fromApi = await api.get<ProfiloDto[]>(
      `/api/Profili/medici${qs ? `?${qs}` : ""}`
    );
    if (Array.isArray(fromApi) && fromApi.length > 0) {
      return fromApi;
    }
  } catch {
    // Fallback sotto: anon key + RLS sul backend può restituire lista vuota.
  }

  return cercaMediciFromDb(params);
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

export function formatTime(iso: string): string {
  try {
    return new Intl.DateTimeFormat("it-IT", {
      hour: "2-digit",
      minute: "2-digit",
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function toLocalDateKey(isoOrDate: string | Date): string {
  const d = typeof isoOrDate === "string" ? new Date(isoOrDate) : isoOrDate;
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function todayLocalDateKey(): string {
  return toLocalDateKey(new Date());
}

/** Guid vuoto restituito dal backend per fasce virtuali non ancora persistite. */
export const EMPTY_SLOT_ID = "00000000-0000-0000-0000-000000000000";

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

export type SlotInterval = { dataOraInizio: string; dataOraFine: string };

export type FasciaRicorrente = {
  weekdays: number[]; // 0=Lun … 6=Dom
  oraInizio: string;
  oraFine: string;
};

export type StagionePreset = "inverno" | "primavera" | "estate" | "autunno";

/** Pasqua (algoritmo di Meeus/Jones/Butcher) → lunedì di Pasquetta. */
function easterMonday(year: number): Date {
  const a = year % 19;
  const b = Math.floor(year / 100);
  const c = year % 100;
  const d = Math.floor(b / 4);
  const e = b % 4;
  const f = Math.floor((b + 8) / 25);
  const g = Math.floor((b - f + 1) / 3);
  const h = (19 * a + b - d - g + 15) % 30;
  const i = Math.floor(c / 4);
  const k = c % 4;
  const l = (32 + 2 * e + 2 * i - h - k) % 7;
  const m = Math.floor((a + 11 * h + 22 * l) / 451);
  const month = Math.floor((h + l - 7 * m + 114) / 31);
  const day = ((h + l - 7 * m + 114) % 31) + 1;
  return new Date(year, month - 1, day + 1);
}

/** Festività nazionali italiane (fisse + Pasqua/Pasquetta). */
export function isItalianHoliday(date: Date): boolean {
  const y = date.getFullYear();
  const m = date.getMonth() + 1;
  const d = date.getDate();
  const fixed = [
    [1, 1],
    [1, 6],
    [4, 25],
    [5, 1],
    [6, 2],
    [8, 15],
    [11, 1],
    [12, 8],
    [12, 25],
    [12, 26],
  ];
  if (fixed.some(([fm, fd]) => fm === m && fd === d)) return true;
  const pasquetta = easterMonday(y);
  return (
    pasquetta.getFullYear() === y &&
    pasquetta.getMonth() + 1 === m &&
    pasquetta.getDate() === d
  );
}

/** Intervallo stagionale (prossima stagione non interamente passata). */
export function stagioneDateRange(
  stagione: StagionePreset,
  ref = new Date()
): { dataInizio: string; dataFine: string } {
  const year = ref.getFullYear();
  const today = toLocalDateKey(ref);

  const candidates: { dataInizio: string; dataFine: string }[] = (() => {
    switch (stagione) {
      case "inverno":
        return [
          { dataInizio: `${year - 1}-12-21`, dataFine: `${year}-03-20` },
          { dataInizio: `${year}-12-21`, dataFine: `${year + 1}-03-20` },
        ];
      case "primavera":
        return [
          { dataInizio: `${year}-03-21`, dataFine: `${year}-06-20` },
          { dataInizio: `${year + 1}-03-21`, dataFine: `${year + 1}-06-20` },
        ];
      case "estate":
        return [
          { dataInizio: `${year}-06-21`, dataFine: `${year}-09-22` },
          { dataInizio: `${year + 1}-06-21`, dataFine: `${year + 1}-09-22` },
        ];
      case "autunno":
        return [
          { dataInizio: `${year}-09-23`, dataFine: `${year}-12-20` },
          { dataInizio: `${year + 1}-09-23`, dataFine: `${year + 1}-12-20` },
        ];
    }
  })();

  const usable =
    candidates.find((c) => c.dataFine >= today) ?? candidates[candidates.length - 1];
  return {
    dataInizio: usable.dataInizio < today ? today : usable.dataInizio,
    dataFine: usable.dataFine,
  };
}

function parseLocalDate(key: string): Date {
  const [y, m, d] = key.split("-").map(Number);
  return new Date(y, m - 1, d);
}

function weekdayMondayFirst(date: Date): number {
  return (date.getDay() + 6) % 7;
}

export function slotsOverlap(
  a: SlotInterval,
  b: { dataOraInizio: string; dataOraFine: string }
): boolean {
  const aStart = new Date(a.dataOraInizio).getTime();
  const aEnd = new Date(a.dataOraFine).getTime();
  const bStart = new Date(b.dataOraInizio).getTime();
  const bEnd = new Date(b.dataOraFine).getTime();
  return aStart < bEnd && aEnd > bStart;
}

/** Esclude intervalli già presenti in agenda o nel passato. */
export function filterCreatableSlots(
  candidati: SlotInterval[],
  esistenti: { dataOraInizio: string; dataOraFine: string }[],
  now = new Date()
): SlotInterval[] {
  const nowMs = now.getTime();
  return candidati.filter((c) => {
    if (new Date(c.dataOraInizio).getTime() < nowMs) return false;
    return !esistenti.some((e) => slotsOverlap(c, e));
  });
}

export function buildBulkSlots(params: {
  data: string;
  oraInizio: string;
  oraFine: string;
  durataMinuti: number;
  /** Se true (default), non genera slot con inizio nel passato. */
  skipPast?: boolean;
}): SlotInterval[] {
  const { data, oraInizio, oraFine, durataMinuti, skipPast = true } = params;
  if (!data || !oraInizio || !oraFine || durataMinuti <= 0) return [];

  const start = new Date(`${data}T${oraInizio}:00`);
  const end = new Date(`${data}T${oraFine}:00`);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || end <= start) {
    return [];
  }

  const now = Date.now();
  const slots: SlotInterval[] = [];
  const cursor = new Date(start);

  while (cursor.getTime() + durataMinuti * 60_000 <= end.getTime()) {
    const slotEnd = new Date(cursor.getTime() + durataMinuti * 60_000);
    if (!skipPast || cursor.getTime() >= now) {
      slots.push({
        dataOraInizio: cursor.toISOString(),
        dataOraFine: slotEnd.toISOString(),
      });
    }
    cursor.setTime(slotEnd.getTime());
  }

  return slots;
}

/** Genera slot su un intervallo di date, per weekday e fasce orarie. */
export function buildRecurringSlots(params: {
  dataInizio: string;
  dataFine: string;
  fasce: FasciaRicorrente[];
  durataMinuti: number;
  escludiFestivi?: boolean;
  skipPast?: boolean;
}): SlotInterval[] {
  const {
    dataInizio,
    dataFine,
    fasce,
    durataMinuti,
    escludiFestivi = true,
    skipPast = true,
  } = params;

  if (!dataInizio || !dataFine || durataMinuti <= 0 || fasce.length === 0) {
    return [];
  }

  const startDay = parseLocalDate(dataInizio);
  const endDay = parseLocalDate(dataFine);
  if (endDay < startDay) return [];

  const slots: SlotInterval[] = [];
  const cursor = new Date(startDay);

  while (cursor <= endDay) {
    const key = toLocalDateKey(cursor);
    const wd = weekdayMondayFirst(cursor);
    const holiday = escludiFestivi && isItalianHoliday(cursor);

    if (!holiday) {
      for (const fascia of fasce) {
        if (!fascia.weekdays.includes(wd)) continue;
        slots.push(
          ...buildBulkSlots({
            data: key,
            oraInizio: fascia.oraInizio,
            oraFine: fascia.oraFine,
            durataMinuti,
            skipPast,
          })
        );
      }
    }

    cursor.setDate(cursor.getDate() + 1);
  }

  return slots;
}
