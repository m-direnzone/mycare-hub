import { api } from "@/src/lib/api";
import type { StatoPrenotazione } from "@/src/types/enums";
import type { PrenotazioneDto } from "@/src/types/dto";

export interface AdminKpiDto {
  totalePazientiRegistrati: number;
  pazientiNuoviMeseCorrente: number;
  pazientiNuoviMesePrecedente: number;
  deltaCrescitaMensilePercentuale: number;
  totaleMediciAttivi: number;
  mediciInAttesaAttivazione: number;
  visitePrenotateMeseCorrente: number;
  visiteCompletateMeseCorrente: number;
  visiteCancellateOSaltateMeseCorrente: number;
  tassoCompletamentoPercentuale: number;
  volumeAffariIntermediatoMese: number;
}

export interface AdminMedicoDto {
  id: string;
  email?: string | null;
  nome: string;
  cognome: string;
  codiceFiscale: string;
  dataNascita: string;
  telefono?: string | null;
  specializzazione: string;
  numeroIscrizioneAlbo: string;
  biografia?: string | null;
  tariffaVisita: number;
  attivo: boolean;
  passwordCambiata: boolean;
  creatoAt: string;
}

export interface AdminMedicoUpdateDto {
  nome: string;
  cognome: string;
  telefono?: string | null;
  specializzazione: string;
  numeroIscrizioneAlbo: string;
  biografia?: string | null;
  tariffaVisita: number;
  attivo: boolean;
}

export interface AdminPazienteDto {
  id: string;
  nome: string;
  cognome: string;
  codiceFiscale: string;
  dataNascita: string;
  telefono?: string | null;
  creatoAt: string;
}

export interface AdminPrenotazioneDto {
  id: string;
  stato: StatoPrenotazione | string;
  dataOraInizio: string;
  dataOraFine: string;
  medicoId: string;
  medicoNomeCompleto: string;
  medicoSpecializzazione: string;
  pazienteId: string;
  pazienteNomeCompleto: string;
  pazienteCodiceFiscale: string;
  tariffaVisita?: number | null;
  creatoAt: string;
}

export interface SpecializzazioneDto {
  id: string;
  nome: string;
  creatoAt: string;
}

export interface AuditLogDto {
  id: string;
  attoreId?: string | null;
  attoreNome?: string | null;
  azione: string;
  entitaTipo?: string | null;
  entitaId?: string | null;
  dettagli?: string | null;
  creatoAt: string;
}

export function fetchAdminKpi() {
  return api.get<AdminKpiDto>("/api/Admin/kpi");
}

export function fetchAdminMedici() {
  return api.get<AdminMedicoDto[]>("/api/Admin/medici");
}

export function updateAdminMedico(id: string, body: AdminMedicoUpdateDto) {
  return api.put<void>(`/api/Admin/medici/${id}`, body);
}

export function softDeleteAdminMedico(id: string) {
  return api.delete<void>(`/api/Admin/medici/${id}`);
}

export function riattivaAdminMedico(id: string) {
  return api.post<void>(`/api/Admin/medici/${id}/riattiva`);
}

export function fetchAdminPazienti() {
  return api.get<AdminPazienteDto[]>("/api/Admin/pazienti");
}

export function fetchAdminPrenotazioni(params?: {
  anno?: number;
  mese?: number;
  giorno?: number;
}) {
  const q = new URLSearchParams();
  if (params?.anno) q.set("anno", String(params.anno));
  if (params?.mese) q.set("mese", String(params.mese));
  if (params?.giorno) q.set("giorno", String(params.giorno));
  const qs = q.toString();
  return api.get<AdminPrenotazioneDto[]>(
    `/api/Admin/prenotazioni${qs ? `?${qs}` : ""}`
  );
}

export function fetchAdminAudit(limit = 200) {
  return api.get<AuditLogDto[]>(`/api/Admin/audit?limit=${limit}`);
}

export function fetchSpecializzazioniCatalogo() {
  return api.get<SpecializzazioneDto[]>("/api/Specializzazioni");
}

export function createSpecializzazione(nome: string) {
  return api.post<SpecializzazioneDto>("/api/Admin/specializzazioni", { nome });
}

export function deleteSpecializzazione(id: string) {
  return api.delete<void>(`/api/Admin/specializzazioni/${id}`);
}

export function formatEuro(value: number): string {
  return new Intl.NumberFormat("it-IT", {
    style: "currency",
    currency: "EUR",
  }).format(value);
}

export function formatDelta(value: number): string {
  const sign = value > 0 ? "+" : "";
  return `${sign}${value.toFixed(1)}%`;
}

export type { PrenotazioneDto };
