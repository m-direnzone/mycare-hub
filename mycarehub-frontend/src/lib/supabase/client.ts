import { createClient, type AuthError, type SupabaseClient, type User } from "@supabase/supabase-js";
import type { PazienteRegistrazioneDto } from "@/src/types/dto";
import type { RuoloUtente } from "@/src/types/enums";

const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL;
const supabaseAnonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

if (!supabaseUrl || !supabaseAnonKey) {
  throw new Error(
    "Missing NEXT_PUBLIC_SUPABASE_URL or NEXT_PUBLIC_SUPABASE_ANON_KEY environment variables."
  );
}

export const supabase: SupabaseClient = createClient(
  supabaseUrl,
  supabaseAnonKey
);

export function needsPasswordChange(user: User | null | undefined): boolean {
  if (!user) return false;

  // app_metadata è la fonte autoritativa (non modificabile dal client).
  // user_metadata resta come fallback per account già creati con il flag lì.
  return (
    user.app_metadata?.must_change_password === true ||
    user.user_metadata?.must_change_password === true
  );
}

export function getUserRuolo(user: User | null | undefined): RuoloUtente | null {
  if (!user) return null;

  const raw =
    user.app_metadata?.ruolo ??
    user.app_metadata?.role ??
    user.user_metadata?.ruolo;

  if (raw === "paziente" || raw === "medico" || raw === "admin") {
    return raw;
  }

  return null;
}

export function isExistingAccountError(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;

  const authError = error as Partial<AuthError> & { message?: string; code?: string };
  const message = (authError.message ?? "").toLowerCase();
  const code = (authError.code ?? "").toLowerCase();

  return (
    code === "user_already_exists" ||
    message.includes("already registered") ||
    message.includes("user already exists") ||
    message.includes("email address has already been registered")
  );
}

export function isCodiceFiscaleTakenError(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;

  const authError = error as Partial<AuthError> & { message?: string; code?: string };
  const message = (authError.message ?? "").toLowerCase();
  const code = (authError.code ?? "").toLowerCase();

  return (
    code === "codice_fiscale_exists" ||
    message.includes("codice_fiscale_gia_registrato") ||
    message.includes("codice fiscale") ||
    (message.includes("duplicate key") && message.includes("codice_fiscale")) ||
    (message.includes("database error saving new user") &&
      message.includes("codice_fiscale"))
  );
}

export const CODICE_FISCALE_GIA_REGISTRATO_MESSAGE =
  "Risulti già registrato: questo codice fiscale è già presente nel sistema. Non creare un nuovo account — effettua il login con le credenziali usate alla prima registrazione.";

export function messageForExistingRuolo(
  ruolo: RuoloUtente | null | undefined,
  kind: "email" | "cf"
): string {
  if (ruolo === "medico") {
    return kind === "email"
      ? "Esiste già un account medico con questa email creato dall'amministrazione. Accedi con la password temporanea ricevuta: al primo accesso ti verrà chiesto di impostarne una nuova."
      : "Risulti già registrato come medico dall'amministrazione. Non creare un nuovo account — effettua il login con la password temporanea ricevuta: al primo accesso ti verrà chiesto di impostarne una nuova.";
  }

  if (ruolo === "paziente") {
    return kind === "email"
      ? "Esiste già un account paziente con questa email. Accedi con le tue credenziali oppure recupera la password."
      : "Risulti già registrato come paziente. Non creare un nuovo account — effettua il login con le credenziali usate alla prima registrazione.";
  }

  if (ruolo === "admin") {
    return kind === "email"
      ? "Esiste già un account con questa email. Accedi con le tue credenziali."
      : "Questo codice fiscale risulta già associato a un account. Effettua il login.";
  }

  return kind === "email"
    ? "Esiste già un account con questa email. Accedi oppure recupera la password."
    : CODICE_FISCALE_GIA_REGISTRATO_MESSAGE;
}

export function mapAuthErrorMessage(error: unknown, fallback: string): string {
  if (isCodiceFiscaleTakenError(error)) {
    return CODICE_FISCALE_GIA_REGISTRATO_MESSAGE;
  }

  if (isExistingAccountError(error)) {
    return "Esiste già un account con questa email. Accedi oppure recupera la password.";
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    const msg = error.message.toLowerCase();
    if (
      msg.includes("database error saving new user") ||
      msg.includes("unexpected_failure") ||
      msg === "failed to fetch"
    ) {
      return "Registrazione non riuscita per un errore del server. Verifica i dati (in particolare il codice fiscale) e riprova.";
    }
    return error.message;
  }

  return fallback;
}

export async function lookupRuoloAccount(options: {
  email?: string | null;
  codiceFiscale?: string | null;
}): Promise<RuoloUtente | null> {
  const { data, error } = await supabase.rpc("lookup_ruolo_account", {
    p_email: options.email?.trim() || null,
    p_cf: options.codiceFiscale?.trim().toUpperCase() || null,
  });

  if (error) {
    console.warn("lookup_ruolo_account failed:", error.message);
    return null;
  }

  if (data === "paziente" || data === "medico" || data === "admin") {
    return data;
  }

  return null;
}

async function assertCodiceFiscaleDisponibile(codiceFiscale: string) {
  const { data, error } = await supabase.rpc("is_codice_fiscale_disponibile", {
    p_cf: codiceFiscale,
  });

  if (error) {
    // Non bloccare la signup se l'RPC non è raggiungibile: il trigger resta la rete di sicurezza.
    console.warn("CF availability check failed:", error.message);
    return;
  }

  if (data === false) {
    const ruolo = await lookupRuoloAccount({ codiceFiscale });
    throw Object.assign(new Error(messageForExistingRuolo(ruolo, "cf")), {
      name: "AuthApiError",
      status: 409,
      code: "codice_fiscale_exists",
      ruolo,
    });
  }
}

export async function login(email: string, password: string) {
  const { data, error } = await supabase.auth.signInWithPassword({
    email,
    password,
  });

  if (error) {
    throw error;
  }

  return data;
}

/** @deprecated Preferire registerPaziente per passare i metadati anagrafici. */
export async function register(email: string, password: string) {
  return registerPaziente({
    email,
    password,
    nome: "Da Completare",
    cognome: "Da Completare",
    codiceFiscale: "NONSPECIFICATO123",
    dataNascita: new Date().toISOString().slice(0, 10),
  });
}

/**
 * Registrazione pubblica: il ruolo è sempre "paziente".
 * I metadati alimentano il trigger PostgreSQL handle_new_user → tabella profili.
 * Se signUp non restituisce sessione (config Auth), stabilisce la sessione via login.
 */
export async function registerPaziente(payload: PazienteRegistrazioneDto) {
  const codiceFiscale = payload.codiceFiscale.trim().toUpperCase();
  await assertCodiceFiscaleDisponibile(codiceFiscale);

  const meta: Record<string, string | boolean> = {
    nome: payload.nome.trim(),
    cognome: payload.cognome.trim(),
    codice_fiscale: codiceFiscale,
    data_nascita: payload.dataNascita,
    ruolo: "paziente",
  };

  const telefono = payload.telefono?.trim();
  if (telefono) {
    meta.telefono = telefono;
  }

  const { data, error } = await supabase.auth.signUp({
    email: payload.email.trim(),
    password: payload.password,
    options: {
      data: meta,
    },
  });

  if (error) {
    throw error;
  }

  // Caso edge: email già presente ma Supabase non solleva errore (identities vuote).
  if (data.user && Array.isArray(data.user.identities) && data.user.identities.length === 0) {
    throw Object.assign(new Error("User already registered"), {
      name: "AuthApiError",
      status: 422,
      code: "user_already_exists",
    });
  }

  if (!data.session) {
    return login(payload.email.trim(), payload.password);
  }

  return data;
}

export async function changePasswordAndClearTempFlag(newPassword: string) {
  const { data, error } = await supabase.auth.updateUser({
    password: newPassword,
    data: {
      must_change_password: false,
    },
  });

  if (error) {
    throw error;
  }

  return data;
}

export async function getCurrentUser() {
  const {
    data: { session },
  } = await supabase.auth.getSession();

  // Evita AuthSessionMissingError quando non c'è ancora una sessione (es. post-signup).
  if (!session) {
    return null;
  }

  const {
    data: { user },
    error,
  } = await supabase.auth.getUser();

  if (error) {
    if (
      error.name === "AuthSessionMissingError" ||
      error.message.toLowerCase().includes("auth session missing")
    ) {
      return null;
    }
    throw error;
  }

  return user;
}

export async function logout() {
  const { error } = await supabase.auth.signOut();

  if (error) {
    throw error;
  }
}
