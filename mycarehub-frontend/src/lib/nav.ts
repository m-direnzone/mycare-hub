import type { RuoloUtente } from "@/src/types/enums";

export type NavItem = {
  href: string;
  label: string;
  section?: string;
};

const PAZIENTE_NAV: NavItem[] = [
  { href: "/dashboard", label: "Panoramica", section: "Generale" },
  { href: "/dashboard/prenotazioni", label: "Le mie prenotazioni", section: "Prenotazioni" },
  { href: "/dashboard/prenotazioni/nuova", label: "Nuova prenotazione", section: "Prenotazioni" },
  { href: "/dashboard/fascicolo", label: "Fascicolo sanitario", section: "Documenti" },
  { href: "/dashboard/profilo", label: "Profilo personale", section: "Account" },
];

const MEDICO_NAV: NavItem[] = [
  { href: "/dashboard", label: "Panoramica", section: "Generale" },
  { href: "/dashboard/agenda", label: "Agenda & slot", section: "Agenda" },
  { href: "/dashboard/visite", label: "Gestione visite", section: "Visite" },
  { href: "/dashboard/referti", label: "Emissione referti", section: "Documenti" },
  { href: "/dashboard/profilo", label: "Profilo personale", section: "Account" },
];

export function navForRuolo(ruolo: RuoloUtente | null | undefined): NavItem[] {
  if (ruolo === "paziente") return PAZIENTE_NAV;
  if (ruolo === "medico") return MEDICO_NAV;
  return [];
}

export function canAccessPath(
  ruolo: RuoloUtente | null | undefined,
  pathname: string
): boolean {
  if (!ruolo || ruolo === "admin") return false;
  const allowed = navForRuolo(ruolo).map((n) => n.href);
  if (pathname === "/dashboard") return true;
  return allowed.some(
    (href) => pathname === href || pathname.startsWith(`${href}/`)
  );
}
