import {
  getCurrentUser,
  needsPasswordChange,
} from "@/src/lib/supabase/client";
import { fetchMioProfiloFromDb } from "@/src/lib/profilo";
import type { ProfiloDto } from "@/src/types/dto";
import type { User } from "@supabase/supabase-js";

export type AdminGateResult =
  | { ok: true; user: User; profilo: ProfiloDto }
  | { ok: false; redirect: "/login" | "/cambia-password" | "/dashboard" };

/**
 * Autorizza solo utenti con profilo.ruolo = admin (tabella profili su Supabase).
 */
export async function requireAdminFromProfilo(): Promise<AdminGateResult> {
  const user = await getCurrentUser();
  if (!user) {
    return { ok: false, redirect: "/login" };
  }

  if (needsPasswordChange(user)) {
    return { ok: false, redirect: "/cambia-password" };
  }

  const profilo = await fetchMioProfiloFromDb();
  if (!profilo || profilo.ruolo !== "admin") {
    return { ok: false, redirect: "/dashboard" };
  }

  return { ok: true, user, profilo };
}
