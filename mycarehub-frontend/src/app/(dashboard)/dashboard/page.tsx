"use client";

import Link from "next/link";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";

export default function DashboardHomePage() {
  const { profilo } = useDashboard();
  const isPaziente = profilo.ruolo === "paziente";

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">
          Ciao, {profilo.nome || "utente"}
        </h1>
        <p className="dash-page-subtitle">
          {isPaziente
            ? "Gestisci prenotazioni, fascicolo sanitario e profilo personale."
            : "Gestisci agenda, visite e emissione referti dalla tua area dedicata."}
        </p>
      </header>

      <div className="dash-grid">
        {isPaziente ? (
          <>
            <Link href="/dashboard/prenotazioni/nuova" className="dash-tile">
              <h2>Nuova prenotazione</h2>
              <p>Cerca un medico e prenota uno slot disponibile.</p>
            </Link>
            <Link href="/dashboard/prenotazioni" className="dash-tile">
              <h2>Le mie prenotazioni</h2>
              <p>Consulta, conferma lo stato o cancella le visite.</p>
            </Link>
            <Link href="/dashboard/fascicolo" className="dash-tile">
              <h2>Fascicolo sanitario</h2>
              <p>Scarica i referti clinici in modo sicuro.</p>
            </Link>
            <Link href="/dashboard/profilo" className="dash-tile">
              <h2>Profilo personale</h2>
              <p>Verifica anagrafica e codice fiscale.</p>
            </Link>
          </>
        ) : (
          <>
            <Link href="/dashboard/agenda" className="dash-tile">
              <h2>Agenda & slot</h2>
              <p>Genera slot in blocco e blocca quelli liberi.</p>
            </Link>
            <Link href="/dashboard/visite" className="dash-tile">
              <h2>Gestione visite</h2>
              <p>Accetta richieste e concludi le visite.</p>
            </Link>
            <Link href="/dashboard/referti" className="dash-tile">
              <h2>Emissione referti</h2>
              <p>Carica PDF collegati a visita e paziente.</p>
            </Link>
            <Link href="/dashboard/profilo" className="dash-tile">
              <h2>Profilo personale</h2>
              <p>Dati anagrafici e professionali.</p>
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
