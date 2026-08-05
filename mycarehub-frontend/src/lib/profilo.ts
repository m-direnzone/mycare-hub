import { supabase } from "@/src/lib/supabase/client";
import type { MedicoDettaglioDto, ProfiloDto } from "@/src/types/dto";
import type { RuoloUtente } from "@/src/types/enums";

type ProfiloRow = {
  id: string;
  nome: string;
  cognome: string;
  codice_fiscale: string;
  data_nascita: string;
  telefono: string | null;
  ruolo: RuoloUtente;
};

type MedicoDettaglioRow = {
  specializzazione: string;
  numero_iscrizione_albo: string;
  biografia: string | null;
  tariffa_visita: number;
  attivo: boolean;
};

function mapProfilo(row: ProfiloRow): ProfiloDto {
  return {
    id: row.id,
    nome: row.nome,
    cognome: row.cognome,
    codiceFiscale: row.codice_fiscale,
    dataNascita: row.data_nascita,
    telefono: row.telefono,
    ruolo: row.ruolo,
  };
}

function mapDettagliMedico(row: MedicoDettaglioRow): MedicoDettaglioDto {
  return {
    specializzazione: row.specializzazione,
    numeroIscrizioneAlbo: row.numero_iscrizione_albo,
    biografia: row.biografia,
    tariffaVisita: Number(row.tariffa_visita),
    attivo: row.attivo,
  };
}

async function fetchDettagliMedico(
  medicoId: string
): Promise<MedicoDettaglioDto | null> {
  const { data, error } = await supabase
    .from("medici_dettagli")
    .select(
      "specializzazione, numero_iscrizione_albo, biografia, tariffa_visita, attivo"
    )
    .eq("medico_id", medicoId)
    .maybeSingle();

  if (error || !data) return null;
  return mapDettagliMedico(data as MedicoDettaglioRow);
}

/**
 * Legge il profilo dalla tabella public.profili (fonte autoritativa del ruolo).
 */
export async function fetchMioProfiloFromDb(): Promise<ProfiloDto | null> {
  const {
    data: { user },
  } = await supabase.auth.getUser();

  if (!user) return null;

  const { data, error } = await supabase
    .from("profili")
    .select("id, nome, cognome, codice_fiscale, data_nascita, telefono, ruolo")
    .eq("id", user.id)
    .maybeSingle();

  if (!error && data) {
    const profilo = mapProfilo(data as ProfiloRow);
    if (profilo.ruolo === "medico") {
      profilo.dettagliMedico = await fetchDettagliMedico(profilo.id);
    }
    return profilo;
  }

  // Fallback: RPC security definer (funziona anche se RLS blocca il SELECT diretto)
  const { data: ruolo, error: rpcError } = await supabase.rpc(
    "lookup_ruolo_account",
    {
      p_email: user.email ?? null,
      p_cf: null,
    }
  );

  if (rpcError || !ruolo) {
    console.warn(
      "fetchMioProfiloFromDb failed:",
      error?.message ?? rpcError?.message
    );
    return null;
  }

  if (ruolo !== "paziente" && ruolo !== "medico" && ruolo !== "admin") {
    return null;
  }

  const profilo: ProfiloDto = {
    id: user.id,
    email: user.email ?? null,
    nome: String(user.user_metadata?.nome ?? ""),
    cognome: String(user.user_metadata?.cognome ?? ""),
    codiceFiscale: String(user.user_metadata?.codice_fiscale ?? ""),
    dataNascita: String(user.user_metadata?.data_nascita ?? ""),
    telefono: (user.user_metadata?.telefono as string | undefined) ?? null,
    ruolo,
  };

  if (ruolo === "medico") {
    profilo.dettagliMedico = await fetchDettagliMedico(user.id);
  }

  return profilo;
}

/**
 * Aggiorna anagrafica del profilo autenticato via Supabase (RLS: profili_update_own).
 * Vale per paziente e medico: il backend con publishable key non ha auth.uid().
 */
export async function updateMioProfiloFromDb(payload: {
  nome: string;
  cognome: string;
  telefono?: string | null;
}): Promise<ProfiloDto> {
  const {
    data: { user },
  } = await supabase.auth.getUser();

  if (!user) {
    throw new Error("Sessione non valida. Effettua di nuovo l'accesso.");
  }

  const nome = payload.nome.trim();
  const cognome = payload.cognome.trim();
  const telefono = payload.telefono?.trim() || null;

  if (!nome || !cognome) {
    throw new Error("Nome e cognome sono obbligatori.");
  }

  const { data, error } = await supabase
    .from("profili")
    .update({
      nome,
      cognome,
      telefono,
      aggiornato_at: new Date().toISOString(),
    })
    .eq("id", user.id)
    .select("id, nome, cognome, codice_fiscale, data_nascita, telefono, ruolo")
    .maybeSingle();

  if (error) {
    throw new Error(error.message || "Aggiornamento non riuscito.");
  }

  if (!data) {
    throw new Error("Profilo non trovato o non aggiornabile.");
  }

  return mapProfilo(data as ProfiloRow);
}

/**
 * Aggiorna la biografia del medico autenticato.
 * RLS: medici_dettagli_update_own.
 */
export async function updateMiaBiografiaMedicoFromDb(payload: {
  biografia?: string | null;
}): Promise<MedicoDettaglioDto> {
  const {
    data: { user },
  } = await supabase.auth.getUser();

  if (!user) {
    throw new Error("Sessione non valida. Effettua di nuovo l'accesso.");
  }

  const biografia = payload.biografia?.trim() || null;

  const { data, error } = await supabase
    .from("medici_dettagli")
    .update({ biografia })
    .eq("medico_id", user.id)
    .select(
      "specializzazione, numero_iscrizione_albo, biografia, tariffa_visita, attivo"
    )
    .maybeSingle();

  if (error) {
    throw new Error(error.message || "Aggiornamento biografia non riuscito.");
  }

  if (!data) {
    throw new Error("Dettagli medico non trovati o non aggiornabili.");
  }

  return mapDettagliMedico(data as MedicoDettaglioRow);
}
