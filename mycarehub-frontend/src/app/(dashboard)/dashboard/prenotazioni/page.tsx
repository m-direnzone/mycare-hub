"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  cambiaStatoPrenotazioneApi,
  fetchMieVisitePazienteApi,
  formatDateTime,
  statoLabel,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto } from "@/src/types/dto";
import { ApiError } from "@/src/lib/api";
import { toast } from "react-toastify";

type FilterKey = "in_attesa" | "confermate" | "oggi" | "questo_mese";

function startOfDay(d = new Date()) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d = new Date()) {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

function startOfMonth(d = new Date()) {
  return new Date(d.getFullYear(), d.getMonth(), 1, 0, 0, 0, 0);
}

function endOfMonth(d = new Date()) {
  return new Date(d.getFullYear(), d.getMonth() + 1, 0, 23, 59, 59, 999);
}

export default function PrenotazioniPazientePage() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [filter, setFilter] = useState<FilterKey>("in_attesa");

  async function load() {
    setLoading(true);
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
      toast.error(
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

  const filtered = useMemo(() => {
    const todayStart = startOfDay().getTime();
    const todayEnd = endOfDay().getTime();
    const monthStart = startOfMonth().getTime();
    const monthEnd = endOfMonth().getTime();

    return visite.filter((v) => {
      const t = new Date(v.dataOraInizio).getTime();
      if (filter === "in_attesa") return v.stato === "in_attesa";
      if (filter === "confermate") return v.stato === "confermata";
      if (filter === "oggi") return t >= todayStart && t <= todayEnd;
      return t >= monthStart && t <= monthEnd;
    });
  }, [visite, filter]);

  async function onCancella(id: string) {
    if (!confirm("Confermi la cancellazione di questa prenotazione?")) return;
    setBusyId(id);
    try {
      await cambiaStatoPrenotazioneApi(id, "cancellata");
      await load();
    } catch (err) {
      toast.error(
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
    <div className="dash-page dash-page-locked">
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

      <div className="dash-tabs" role="tablist" aria-label="Filtri prenotazioni">
        {(
          [
            ["in_attesa", "In attesa"],
            ["confermate", "Confermate"],
            ["oggi", "Oggi"],
            ["questo_mese", "Questo mese"],
          ] as const
        ).map(([key, label]) => (
          <button
            key={key}
            type="button"
            role="tab"
            aria-selected={filter === key}
            className={`dash-tab ${filter === key ? "is-active" : ""}`}
            onClick={() => setFilter(key)}
          >
            {label}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-sm text-muted">Caricamento…</p>
      ) : visite.length === 0 ? (
        <div className="dash-empty">
          <p>Nessuna prenotazione presente.</p>
          <Link href="/dashboard/prenotazioni/nuova" className="auth-link">
            Prenota la prima visita
          </Link>
        </div>
      ) : filtered.length === 0 ? (
        <div className="dash-empty">
          <p>Nessuna prenotazione per questo filtro.</p>
        </div>
      ) : (
        <ul className="dash-list dash-list-scrollable">
          {filtered.map((v) => (
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
                {v.stato !== "cancellata" && (
                  <Link
                    href={`/dashboard/chat?medicoId=${encodeURIComponent(v.medicoId)}`}
                    className="dash-btn dash-btn-ghost dash-btn-nowrap"
                  >
                    Chatta
                  </Link>
                )}
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
