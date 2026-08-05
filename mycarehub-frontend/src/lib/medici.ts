import { ApiError, api } from "@/src/lib/api";
import { supabase } from "@/src/lib/supabase/client";
import type { MedicoCreateResponseDto, MedicoOnboardingDto } from "@/src/types/dto";

function extractErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError) {
    const body = err.body as { message?: string } | string | null;
    if (typeof body === "object" && body !== null && typeof body.message === "string") {
      return body.message;
    }
    if (typeof body === "string" && body.trim().length > 0) {
      return body;
    }
    return err.message || fallback;
  }

  if (err instanceof Error && err.message.trim().length > 0) {
    return err.message;
  }

  return fallback;
}

/**
 * Crea un medico tramite Edge Function (verifica JWT admin lato Supabase).
 * La service role resta solo nei secret della Function, non nel backend ASP.NET.
 */
export async function createMedico(
  payload: MedicoOnboardingDto
): Promise<MedicoCreateResponseDto> {
  const { data, error } = await supabase.functions.invoke("admin-create-medico", {
    body: payload,
  });

  if (error) {
    let detail = error.message || "Creazione medico non riuscita.";
    try {
      const ctx = (error as { context?: Response }).context;
      if (ctx) {
        const body = await ctx.json();
        if (body && typeof body.message === "string") {
          detail = body.message;
        }
      }
    } catch {
      // ignore parse errors
    }
    throw new Error(detail);
  }

  if (data && typeof data === "object" && "message" in data && !("id" in data)) {
    throw new Error(String((data as { message: string }).message));
  }

  // Audit best-effort via API JWT (non blocca la creazione).
  try {
    await api.post("/api/Admin/audit/medico-creato", {
      medicoId: (data as MedicoCreateResponseDto).id,
      email: (data as MedicoCreateResponseDto).email,
      nome: payload.nome,
      cognome: payload.cognome,
    });
  } catch {
    // ignore
  }

  return data as MedicoCreateResponseDto;
}

export { extractErrorMessage };
