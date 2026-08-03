"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError } from "@/src/lib/api";
import {
  cercaMediciApi,
  creaPrenotazioneApi,
  fetchSlotDisponibiliApi,
  formatDateTime,
} from "@/src/lib/dashboard-api";
import type { ProfiloDto, SlotDto } from "@/src/types/dto";

export default function NuovaPrenotazionePage() {
  const router = useRouter();
  const [specializzazione, setSpecializzazione] = useState("");
  const [nome, setNome] = useState("");
  const [medici, setMedici] = useState<ProfiloDto[]>([]);
  const [selectedMedico, setSelectedMedico] = useState<ProfiloDto | null>(null);
  const [slots, setSlots] = useState<SlotDto[]>([]);
  const [selectedSlotId, setSelectedSlotId] = useState<string | null>(null);
  const [notePaziente, setNotePaziente] = useState("");
  const [dataFiltro, setDataFiltro] = useState("");
  const [searching, setSearching] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  async function onSearch(e?: React.FormEvent) {
    e?.preventDefault();
    setSearching(true);
    setError(null);
    setMessage(null);
    setSelectedMedico(null);
    setSlots([]);
    setSelectedSlotId(null);
    try {
      const results = await cercaMediciApi({ specializzazione, nome });
      setMedici(results);
      if (results.length === 0) {
        setMessage("Nessun medico trovato con i filtri selezionati.");
      }
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Ricerca medici non riuscita."
      );
    } finally {
      setSearching(false);
    }
  }

  useEffect(() => {
    void onSearch();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!selectedMedico) return;
    let active = true;

    (async () => {
      setLoadingSlots(true);
      setError(null);
      setSelectedSlotId(null);
      try {
        let dataInizio: string | undefined;
        let dataFine: string | undefined;
        if (dataFiltro) {
          dataInizio = new Date(`${dataFiltro}T00:00:00`).toISOString();
          dataFine = new Date(`${dataFiltro}T23:59:59`).toISOString();
        }
        const data = await fetchSlotDisponibiliApi(
          selectedMedico.id,
          dataInizio,
          dataFine
        );
        if (active) setSlots(data);
      } catch (err) {
        if (active) {
          setError(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare gli slot."
          );
          setSlots([]);
        }
      } finally {
        if (active) setLoadingSlots(false);
      }
    })();

    return () => {
      active = false;
    };
  }, [selectedMedico, dataFiltro]);

  const slotsByDay = useMemo(() => {
    const map = new Map<string, SlotDto[]>();
    for (const slot of slots) {
      const day = new Date(slot.dataOraInizio).toLocaleDateString("it-IT");
      const list = map.get(day) ?? [];
      list.push(slot);
      map.set(day, list);
    }
    return [...map.entries()];
  }, [slots]);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedMedico || !selectedSlotId) {
      setError("Seleziona un medico e uno slot disponibile.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await creaPrenotazioneApi({
        slotId: selectedSlotId,
        medicoId: selectedMedico.id,
        notePaziente: notePaziente.trim() || null,
      });
      router.replace("/dashboard/prenotazioni");
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : "Creazione prenotazione non riuscita."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Nuova prenotazione</h1>
        <p className="dash-page-subtitle">
          Cerca un medico, scegli uno slot e inserisci le note sintomatiche.
        </p>
      </header>

      {error && <p className="auth-alert">{error}</p>}
      {message && <p className="auth-alert auth-alert-info">{message}</p>}

      <form className="dash-panel space-y-4" onSubmit={onSearch}>
        <h2 className="dash-section-title">1. Ricerca medici</h2>
        <div className="dash-form-grid">
          <label className="dash-label">
            Specializzazione
            <input
              className="auth-field"
              value={specializzazione}
              onChange={(e) => setSpecializzazione(e.target.value)}
              placeholder="es. Cardiologia"
            />
          </label>
          <label className="dash-label">
            Nome / cognome
            <input
              className="auth-field"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              placeholder="es. Rossi"
            />
          </label>
        </div>
        <button className="dash-btn" type="submit" disabled={searching}>
          {searching ? "Ricerca…" : "Cerca"}
        </button>

        {medici.length > 0 && (
          <ul className="dash-list">
            {medici.map((m) => (
              <li key={m.id}>
                <button
                  type="button"
                  className={`dash-select-card ${
                    selectedMedico?.id === m.id ? "is-selected" : ""
                  }`}
                  onClick={() => setSelectedMedico(m)}
                >
                  <strong>
                    {m.nome} {m.cognome}
                  </strong>
                  <span>
                    {m.dettagliMedico?.specializzazione ?? "Specialista"}
                    {m.dettagliMedico
                      ? ` · € ${m.dettagliMedico.tariffaVisita}`
                      : ""}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </form>

      {selectedMedico && (
        <form className="dash-panel space-y-4" onSubmit={onSubmit}>
          <h2 className="dash-section-title">2. Calendario slot</h2>
          <label className="dash-label">
            Filtra per data
            <input
              type="date"
              className="auth-field"
              value={dataFiltro}
              onChange={(e) => setDataFiltro(e.target.value)}
            />
          </label>

          {loadingSlots ? (
            <p className="text-sm text-muted">Caricamento slot…</p>
          ) : slots.length === 0 ? (
            <p className="text-sm text-muted">
              Nessuno slot disponibile per il periodo selezionato.
            </p>
          ) : (
            <div className="space-y-4">
              {slotsByDay.map(([day, daySlots]) => (
                <div key={day}>
                  <p className="dash-day-label">{day}</p>
                  <div className="dash-slot-grid">
                    {daySlots.map((slot) => (
                      <button
                        key={slot.id}
                        type="button"
                        className={`dash-slot ${
                          selectedSlotId === slot.id ? "is-selected" : ""
                        }`}
                        onClick={() => setSelectedSlotId(slot.id)}
                      >
                        {formatDateTime(slot.dataOraInizio)}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          <h2 className="dash-section-title">3. Note sintomatiche</h2>
          <label className="dash-label">
            Descrizione sintomi / note
            <textarea
              className="auth-field dash-textarea"
              rows={4}
              value={notePaziente}
              onChange={(e) => setNotePaziente(e.target.value)}
              placeholder="Descrivi brevemente i sintomi o le motivazioni della visita"
            />
          </label>

          <button
            className="dash-btn"
            type="submit"
            disabled={submitting || !selectedSlotId}
          >
            {submitting ? "Prenotazione…" : "Conferma prenotazione"}
          </button>
        </form>
      )}
    </div>
  );
}
