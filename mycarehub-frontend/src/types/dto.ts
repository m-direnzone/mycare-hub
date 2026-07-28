import type { RuoloUtente, StatoPrenotazione } from "./enums";

/** Credenziali per l'autenticazione (Supabase Auth). */
export interface LoginDto {
  email: string;
  password: string;
}

/** Payload inviato dal form di registrazione pubblica del paziente. */
export interface PazienteRegistrazioneDto {
  email: string;
  password: string;
  nome: string;
  cognome: string;
  codiceFiscale: string;
  /** Formato ISO date: YYYY-MM-DD */
  dataNascita: string;
  telefono?: string | null;
}

/** Payload inviato da Admin/Segreteria per registrare ufficialmente un medico. */
export interface MedicoOnboardingDto {
  email: string;
  nome: string;
  cognome: string;
  codiceFiscale: string;
  /** Formato ISO date: YYYY-MM-DD */
  dataNascita: string;
  telefono?: string | null;
  specializzazione: string;
  numeroIscrizioneAlbo: string;
  biografia?: string | null;
  tariffaVisita: number;
}

/** Risposta dopo creazione completa di un medico. */
export interface MedicoCreateResponseDto {
  id: string;
  email: string;
  passwordTemporanea: string;
  messaggio: string;
}

/** Payload omnicomprensivo restituito dopo il login. */
export interface ProfiloDto {
  id: string;
  email?: string | null;
  nome: string;
  cognome: string;
  codiceFiscale: string;
  /** Formato ISO date: YYYY-MM-DD */
  dataNascita: string;
  telefono?: string | null;
  ruolo: RuoloUtente;
  dettagliMedico?: MedicoDettaglioDto | null;
}

/** Dettagli professionali del medico. */
export interface MedicoDettaglioDto {
  specializzazione: string;
  numeroIscrizioneAlbo: string;
  biografia?: string | null;
  tariffaVisita: number;
  attivo: boolean;
}

/** Aggiornamento anagrafico del profilo autenticato. */
export interface ProfiloUpdateDto {
  nome: string;
  cognome: string;
  telefono?: string | null;
}

/** Aggiornamento dei dettagli professionali del medico. */
export interface MedicoDettaglioUpdateDto {
  specializzazione: string;
  biografia?: string | null;
  tariffaVisita: number;
  attivo: boolean;
}

/** Slot orario restituito al frontend. */
export interface SlotDto {
  id: string;
  medicoId: string;
  dataOraInizio: string;
  dataOraFine: string;
  isPrenotato: boolean;
}

/** Intervallo temporale singolo in una creazione bulk. */
export interface TimeIntervalDto {
  dataOraInizio: string;
  dataOraFine: string;
}

/** Inserimento massivo di slot liberi in agenda. */
export interface SlotCreationBulkDto {
  medicoId: string;
  slotRichiesti: TimeIntervalDto[];
}

/** Payload per bloccare uno slot e confermare la visita. */
export interface PrenotazioneCreateDto {
  slotId: string;
  medicoId: string;
  notePaziente?: string | null;
}

/** Payload per chiudere la visita e salvare il referto testuale. */
export interface PrenotazioneConclusioneDto {
  noteMediche: string;
}

/** Vista denormalizzata di una prenotazione. */
export interface PrenotazioneDto {
  id: string;
  slotId: string;
  stato: StatoPrenotazione;
  notePaziente?: string | null;
  noteMediche?: string | null;
  creatoAt: string;
  aggiornatoAt: string;
  dataOraInizio: string;
  dataOraFine: string;
  medicoId: string;
  medicoNomeCompleto: string;
  medicoSpecializzazione: string;
  pazienteId: string;
  pazienteNomeCompleto: string;
  pazienteCodiceFiscale: string;
}

/** Metadati del referto (multipart/form-data). */
export interface RefertoCreateDto {
  prenotazioneId?: string | null;
  pazienteId: string;
  titolo: string;
  descrizione?: string | null;
}

/** Documento clinico per il Fascicolo Sanitario Elettronico. */
export interface RefertoDto {
  id: string;
  prenotazioneId?: string | null;
  pazienteId: string;
  medicoId: string;
  titolo: string;
  descrizione?: string | null;
  /** Formato ISO date: YYYY-MM-DD */
  dataEmissione: string;
  downloadUrl: string;
  medicoNomeCompleto: string;
  medicoSpecializzazione: string;
}
