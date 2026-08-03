import { supabase } from "@/src/lib/supabase/client";
import type { ProfiloDto } from "@/src/types/dto";
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
    return mapProfilo(data as ProfiloRow);
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

  return {
    id: user.id,
    email: user.email ?? null,
    nome: String(user.user_metadata?.nome ?? ""),
    cognome: String(user.user_metadata?.cognome ?? ""),
    codiceFiscale: String(user.user_metadata?.codice_fiscale ?? ""),
    dataNascita: String(user.user_metadata?.data_nascita ?? ""),
    telefono: (user.user_metadata?.telefono as string | undefined) ?? null,
    ruolo,
  };
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
