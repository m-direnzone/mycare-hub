"use client";

import { useEffect, useState } from "react";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import { ApiError } from "@/src/lib/api";
import {
  buildBulkSlots,
  creaSlotBulkApi,
  eliminaSlotApi,
  fetchMiaAgendaSlotApi,
  formatDateTime,
} from "@/src/lib/dashboard-api";
import type { SlotDto } from "@/src/types/dto";

export default function AgendaMedicoPage() {
  const { profilo } = useDashboard();
  const [slots, setSlots] = useState<SlotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);

  const [data, setData] = useState("");
  const [oraInizio, setOraInizio] = useState("09:00");
  const [oraFine, setOraFine] = useState("13:00");
  const [durata, setDurata] = useState(30);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const dataSlots = await fetchMiaAgendaSlotApi();
      setSlots(
        [...dataSlots].sort(
          (a, b) =>
            new Date(a.dataOraInizio).getTime() -
            new Date(b.dataOraInizio).getTime()
        )
      );
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Impossibile caricare l'agenda."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onGenerate(e: React.FormEvent) {
    e.preventDefault();
    setGenerating(true);
    setError(null);
    setOk(null);
    try {
      const slotRichiesti = buildBulkSlots({
        data,
        oraInizio,
        oraFine,
        durataMinuti: durata,
      });
      if (slotRichiesti.length === 0) {
        setError("Intervallo non valido: nessun slot generabile.");
        return;
      }
      const result = await creaSlotBulkApi({
        medicoId: profilo.id,
        slotRichiesti,
      });
      setOk(`Creati ${result.slotCreati} slot.`);
      await load();
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Generazione massiva non riuscita."
      );
    } finally {
      setGenerating(false);
    }
  }

  async function onBlocca(slotId: string) {
    if (!confirm("Bloccare (eliminare) questo slot libero?")) return;
    setBusyId(slotId);
    setError(null);
    try {
      await eliminaSlotApi(slotId);
      await load();
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Blocco slot non riuscito."
      );
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Gestione agenda & slot</h1>
        <p className="dash-page-subtitle">
          Generazione massiva e blocco degli slot liberi.
        </p>
      </header>

      {error && <p className="auth-alert">{error}</p>}
      {ok && <p className="auth-alert auth-alert-info">{ok}</p>}

      <form className="dash-panel space-y-4" onSubmit={onGenerate}>
        <h2 className="dash-section-title">Bulk generation</h2>
        <div className="dash-form-grid">
          <label className="dash-label">
            Data
            <input
              type="date"
              className="auth-field"
              value={data}
              onChange={(e) => setData(e.target.value)}
              required
            />
          </label>
          <label className="dash-label">
            Ora inizio
            <input
              type="time"
              className="auth-field"
              value={oraInizio}
              onChange={(e) => setOraInizio(e.target.value)}
              required
            />
          </label>
          <label className="dash-label">
            Ora fine
            <input
              type="time"
              className="auth-field"
              value={oraFine}
              onChange={(e) => setOraFine(e.target.value)}
              required
            />
          </label>
          <label className="dash-label">
            Durata visita (minuti)
            <input
              type="number"
              min={5}
              step={5}
              className="auth-field"
              value={durata}
              onChange={(e) => setDurata(Number(e.target.value))}
              required
            />
          </label>
        </div>
        <button className="dash-btn" type="submit" disabled={generating}>
          {generating ? "Generazione…" : "Genera slot"}
        </button>
      </form>

      <section className="dash-panel space-y-3">
        <h2 className="dash-section-title">Agenda</h2>
        {loading ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : slots.length === 0 ? (
          <p className="text-sm text-muted">Nessuno slot in agenda.</p>
        ) : (
          <ul className="dash-list">
            {slots.map((slot) => (
              <li key={slot.id} className="dash-list-item">
                <div className="dash-list-main">
                  <p className="dash-list-title">
                    {formatDateTime(slot.dataOraInizio)} →{" "}
                    {formatDateTime(slot.dataOraFine)}
                  </p>
                </div>
                <div className="dash-list-actions">
                  <span
                    className={`dash-badge ${
                      slot.isPrenotato
                        ? "dash-badge-confermata"
                        : "dash-badge-in_attesa"
                    }`}
                  >
                    {slot.isPrenotato ? "Prenotato" : "Libero"}
                  </span>
                  {!slot.isPrenotato && (
                    <button
                      type="button"
                      className="dash-btn dash-btn-danger"
                      disabled={busyId === slot.id}
                      onClick={() => void onBlocca(slot.id)}
                    >
                      {busyId === slot.id ? "…" : "Blocca"}
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
