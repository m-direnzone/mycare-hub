import { fetchMioProfiloFromDb } from "@/src/lib/profilo";

/**
 * Destinazione post-login basata su profili.ruolo (Supabase).
 */
export async function resolveHomePathFromProfilo(): Promise<"/admin" | "/dashboard"> {
  const profilo = await fetchMioProfiloFromDb();
  return profilo?.ruolo === "admin" ? "/admin" : "/dashboard";
}
