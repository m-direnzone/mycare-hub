import { fetchSpecializzazioniCatalogo } from "@/src/lib/admin-api";

/** Fallback locale se l'API non è disponibile. */
export const SPECIALIZZAZIONI_FALLBACK = [
  "Allergologia",
  "Anestesia e rianimazione",
  "Cardiologia",
  "Chirurgia generale",
  "Dermatologia",
  "Endocrinologia",
  "Gastroenterologia",
  "Geriatria",
  "Ginecologia",
  "Medicina interna",
  "Neurologia",
  "Oculistica",
  "Ortopedia",
  "Otorinolaringoiatria",
  "Pediatria",
  "Pneumologia",
  "Psichiatria",
  "Radiologia",
  "Reumatologia",
  "Urologia",
] as const;

/** @deprecated Preferisci fetchSpecializzazioniNomi() */
export const SPECIALIZZAZIONI = SPECIALIZZAZIONI_FALLBACK;

export async function fetchSpecializzazioniNomi(): Promise<string[]> {
  try {
    const rows = await fetchSpecializzazioniCatalogo();
    const nomi = rows.map((r) => r.nome).filter(Boolean);
    if (nomi.length > 0) return nomi;
  } catch {
    // fallback
  }
  return [...SPECIALIZZAZIONI_FALLBACK];
}
