import "jsr:@supabase/functions-js/edge-runtime.d.ts";
import { createClient } from "jsr:@supabase/supabase-js@2";

const CF_REGEX = /^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$/;

type MedicoPayload = {
  email?: string;
  nome?: string;
  cognome?: string;
  codiceFiscale?: string;
  dataNascita?: string;
  telefono?: string | null;
  specializzazione?: string;
  numeroIscrizioneAlbo?: string;
  biografia?: string | null;
  tariffaVisita?: number;
};

function json(status: number, body: Record<string, unknown>) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "Content-Type": "application/json",
      Connection: "keep-alive",
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Headers":
        "authorization, x-client-info, apikey, content-type",
    },
  });
}

function generateTempPassword(length = 14): string {
  const alphabet =
    "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
  const bytes = crypto.getRandomValues(new Uint8Array(length));
  return Array.from(bytes, (b) => alphabet[b % alphabet.length]).join("");
}

Deno.serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response(null, {
      status: 204,
      headers: {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Headers":
          "authorization, x-client-info, apikey, content-type",
        "Access-Control-Allow-Methods": "POST, OPTIONS",
      },
    });
  }

  if (req.method !== "POST") {
    return json(405, { message: "Metodo non consentito." });
  }

  const supabaseUrl = Deno.env.get("SUPABASE_URL");
  const serviceRoleKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY");
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY");

  if (!supabaseUrl || !serviceRoleKey || !anonKey) {
    return json(500, { message: "Configurazione Supabase mancante." });
  }

  const authHeader = req.headers.get("Authorization");
  if (!authHeader) {
    return json(401, { message: "Autenticazione richiesta." });
  }

  const callerClient = createClient(supabaseUrl, anonKey, {
    global: { headers: { Authorization: authHeader } },
  });

  const {
    data: { user: caller },
    error: callerError,
  } = await callerClient.auth.getUser();

  if (callerError || !caller) {
    return json(401, { message: "Sessione non valida." });
  }

  const { data: profiloCaller } = await callerClient
    .from("profili")
    .select("ruolo, nome, cognome")
    .eq("id", caller.id)
    .maybeSingle();

  const ruoloProfilo = String(profiloCaller?.ruolo ?? "").toLowerCase();
  const ruoloMeta =
    (caller.app_metadata?.ruolo as string | undefined) ??
    (caller.app_metadata?.role as string | undefined) ??
    (caller.user_metadata?.ruolo as string | undefined);

  if (ruoloProfilo !== "admin" && String(ruoloMeta).toLowerCase() !== "admin") {
    return json(403, {
      message: "Solo un amministratore può creare un medico.",
    });
  }

  let payload: MedicoPayload;
  try {
    payload = await req.json();
  } catch {
    return json(400, { message: "Body JSON non valido." });
  }

  const email = payload.email?.trim().toLowerCase() ?? "";
  const nome = payload.nome?.trim() ?? "";
  const cognome = payload.cognome?.trim() ?? "";
  const codiceFiscale = (payload.codiceFiscale?.trim() ?? "").toUpperCase();
  const dataNascita = payload.dataNascita?.trim() ?? "";
  const telefono = payload.telefono?.trim() || null;
  const specializzazione = payload.specializzazione?.trim() ?? "";
  const numeroIscrizioneAlbo = payload.numeroIscrizioneAlbo?.trim() ?? "";
  const biografia = payload.biografia?.trim() || null;
  const tariffaVisita = Number(payload.tariffaVisita);

  if (!email || !nome || !cognome || !codiceFiscale || !dataNascita) {
    return json(400, { message: "Campi anagrafici obbligatori mancanti." });
  }
  if (!CF_REGEX.test(codiceFiscale)) {
    return json(400, { message: "Formato codice fiscale non valido." });
  }
  if (!specializzazione || !numeroIscrizioneAlbo) {
    return json(400, {
      message: "Specializzazione e numero albo sono obbligatori.",
    });
  }
  if (!Number.isFinite(tariffaVisita) || tariffaVisita < 0.01 || tariffaVisita > 10000) {
    return json(400, { message: "Tariffa visita non valida." });
  }

  const admin = createClient(supabaseUrl, serviceRoleKey, {
    auth: { autoRefreshToken: false, persistSession: false },
  });

  const { data: cfTaken } = await admin.rpc("is_codice_fiscale_disponibile", {
    p_cf: codiceFiscale,
  });
  if (cfTaken === false) {
    return json(409, {
      message: "Codice fiscale già registrato nel sistema.",
    });
  }

  const { data: alboEsistente, error: alboError } = await admin
    .from("medici_dettagli")
    .select("medico_id")
    .eq("numero_iscrizione_albo", numeroIscrizioneAlbo)
    .maybeSingle();

  if (alboError) {
    return json(500, { message: alboError.message });
  }
  if (alboEsistente) {
    return json(409, {
      message: "Numero di iscrizione all'albo già presente.",
    });
  }

  const passwordTemporanea = generateTempPassword();

  const { data: created, error: createError } = await admin.auth.admin.createUser({
    email,
    password: passwordTemporanea,
    email_confirm: true,
    user_metadata: {
      nome,
      cognome,
      codice_fiscale: codiceFiscale,
      data_nascita: dataNascita,
      telefono,
      ruolo: "medico",
      must_change_password: true,
    },
    app_metadata: {
      ruolo: "medico",
    },
  });

  if (createError || !created.user) {
    const msg = (createError?.message ?? "").toLowerCase();
    if (msg.includes("already") || msg.includes("registered")) {
      return json(409, { message: "Esiste già un account con questa email." });
    }
    return json(400, {
      message: createError?.message ?? "Impossibile creare l'utente medico.",
    });
  }

  const medicoId = created.user.id;

  const { error: profiloError } = await admin
    .from("profili")
    .update({
      nome,
      cognome,
      codice_fiscale: codiceFiscale,
      data_nascita: dataNascita,
      telefono,
      ruolo: "medico",
      aggiornato_at: new Date().toISOString(),
    })
    .eq("id", medicoId);

  if (profiloError) {
    await admin.auth.admin.deleteUser(medicoId);
    return json(400, {
      message: profiloError.message.includes("codice_fiscale")
        ? "Codice fiscale già registrato nel sistema."
        : profiloError.message,
    });
  }

  const { error: dettagliError } = await admin.from("medici_dettagli").insert({
    medico_id: medicoId,
    specializzazione,
    numero_iscrizione_albo: numeroIscrizioneAlbo,
    biografia,
    tariffa_visita: tariffaVisita,
    attivo: true,
    password_cambiata: false,
  });

  if (dettagliError) {
    await admin.auth.admin.deleteUser(medicoId);
    return json(400, {
      message: dettagliError.message.includes("numero_iscrizione_albo")
        ? "Numero di iscrizione all'albo già presente."
        : dettagliError.message,
    });
  }

  const attoreNome = profiloCaller
    ? `${profiloCaller.nome ?? ""} ${profiloCaller.cognome ?? ""}`.trim() || "Admin"
    : "Admin";

  await admin.from("audit_log").insert({
    attore_id: caller.id,
    attore_nome: attoreNome,
    azione: "Creazione medico",
    entita_tipo: "medico",
    entita_id: medicoId,
    dettagli: `L'amministratore ${attoreNome} ha creato il medico ${nome} ${cognome} (${email}).`,
  });

  return json(201, {
    id: medicoId,
    email,
    passwordTemporanea,
    messaggio:
      "Medico creato. Comunica la password temporanea: al primo accesso dovrà cambiarla.",
  });
});
