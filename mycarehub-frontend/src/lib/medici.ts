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

function shouldFallbackToEdge(err: unknown): boolean {
  if (err instanceof TypeError) return true;

  if (err instanceof ApiError) {
    return (
      err.status === 503 ||
      err.status === 401 ||
      err.status === 403 ||
      err.message.toLowerCase().includes("failed to fetch") ||
      err.message.toLowerCase().includes("servicerolekey")
    );
  }

  if (err instanceof Error) {
    const msg = err.message.toLowerCase();
    return msg.includes("failed to fetch") || msg.includes("servicerolekey");
  }

  return false;
}

/**
 * Crea un medico: prova il backend ASP.NET, in fallback la Edge Function Supabase.
 */
export async function createMedico(
  payload: MedicoOnboardingDto
): Promise<MedicoCreateResponseDto> {
  try {
    return await api.post<MedicoCreateResponseDto>("/api/Profili/medici", payload);
  } catch (err) {
    if (!shouldFallbackToEdge(err)) {
      throw new Error(extractErrorMessage(err, "Creazione medico non riuscita."));
    }
  }

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

  return data as MedicoCreateResponseDto;
}
