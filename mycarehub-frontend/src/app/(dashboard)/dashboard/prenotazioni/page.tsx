"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  cambiaStatoPrenotazioneApi,
  fetchMieVisitePazienteApi,
  formatDateTime,
  statoLabel,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto } from "@/src/types/dto";
import { ApiError } from "@/src/lib/api";

export default function PrenotazioniPazientePage() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchMieVisitePazienteApi();
      setVisite(
        [...data].sort(
          (a, b) =>
            new Date(b.dataOraInizio).getTime() -
            new Date(a.dataOraInizio).getTime()
        )
      );
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Impossibile caricare le prenotazioni."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onCancella(id: string) {
    if (!confirm("Confermi la cancellazione di questa prenotazione?")) return;
    setBusyId(id);
    setError(null);
    try {
      await cambiaStatoPrenotazioneApi(id, "cancellata");
      await load();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Cancellazione non riuscita."
      );
    } finally {
      setBusyId(null);
    }
  }

  const cancellabili = new Set(["in_attesa", "confermata"]);

  return (
    <div className="dash-page">
      <header className="dash-page-header dash-page-header-row">
        <div>
          <h1 className="dash-page-title">Le mie prenotazioni</h1>
          <p className="dash-page-subtitle">
            Gestisci le visite in attesa o confermate.
          </p>
        </div>
        <Link href="/dashboard/prenotazioni/nuova" className="dash-btn">
          Nuova prenotazione
        </Link>
      </header>

      {error && <p className="auth-alert">{error}</p>}

      {loading ? (
        <p className="text-sm text-muted">Caricamento…</p>
      ) : visite.length === 0 ? (
        <div className="dash-empty">
          <p>Nessuna prenotazione presente.</p>
          <Link href="/dashboard/prenotazioni/nuova" className="auth-link">
            Prenota la prima visita
          </Link>
        </div>
      ) : (
        <ul className="dash-list">
          {visite.map((v) => (
            <li key={v.id} className="dash-list-item">
              <div className="dash-list-main">
                <p className="dash-list-title">
                  {v.medicoNomeCompleto} · {v.medicoSpecializzazione}
                </p>
                <p className="dash-list-meta">
                  {formatDateTime(v.dataOraInizio)} →{" "}
                  {formatDateTime(v.dataOraFine)}
                </p>
                {v.notePaziente && (
                  <p className="dash-list-note">Note: {v.notePaziente}</p>
                )}
              </div>
              <div className="dash-list-actions">
                <span className={`dash-badge dash-badge-${v.stato}`}>
                  {statoLabel(v.stato)}
                </span>
                {cancellabili.has(v.stato) && (
                  <button
                    type="button"
                    className="dash-btn dash-btn-danger"
                    disabled={busyId === v.id}
                    onClick={() => void onCancella(v.id)}
                  >
                    {busyId === v.id ? "…" : "Cancella"}
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
