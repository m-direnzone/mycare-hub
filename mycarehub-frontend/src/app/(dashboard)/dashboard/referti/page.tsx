"use client";

import { useEffect, useMemo, useState } from "react";
import { ApiError } from "@/src/lib/api";
import {
  caricaRefertoApi,
  fetchAgendaMedicoApi,
  formatDateTime,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto } from "@/src/types/dto";

export default function RefertiMedicoPage() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [prenotazioneId, setPrenotazioneId] = useState("");
  const [titolo, setTitolo] = useState("");
  const [descrizione, setDescrizione] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [ok, setOk] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await fetchAgendaMedicoApi();
        if (active) {
          setVisite(
            data.filter(
              (v) => v.stato === "confermata" || v.stato === "completata"
            )
          );
        }
      } catch (err) {
        if (active) {
          setError(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare le visite."
          );
        }
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  const selected = useMemo(
    () => visite.find((v) => v.id === prenotazioneId) ?? null,
    [visite, prenotazioneId]
  );

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selected || !file) {
      setError("Seleziona una visita e un file PDF.");
      return;
    }
    if (!file.name.toLowerCase().endsWith(".pdf")) {
      setError("Sono accettati solo file PDF.");
      return;
    }

    setSubmitting(true);
    setError(null);
    setOk(null);
    try {
      const form = new FormData();
      form.append("prenotazioneId", selected.id);
      form.append("pazienteId", selected.pazienteId);
      form.append("titolo", titolo.trim());
      if (descrizione.trim()) form.append("descrizione", descrizione.trim());
      form.append("file", file);

      await caricaRefertoApi(form);
      setOk("Referto caricato correttamente.");
      setTitolo("");
      setDescrizione("");
      setFile(null);
      setPrenotazioneId("");
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : "Upload referto non riuscito."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Emissione referti</h1>
        <p className="dash-page-subtitle">
          Carica un PDF collegato automaticamente a visita e paziente.
        </p>
      </header>

      {error && <p className="auth-alert">{error}</p>}
      {ok && <p className="auth-alert auth-alert-info">{ok}</p>}

      <form className="dash-panel space-y-4" onSubmit={onSubmit}>
        <label className="dash-label">
          Visita collegata
          <select
            className="auth-field"
            value={prenotazioneId}
            onChange={(e) => setPrenotazioneId(e.target.value)}
            required
            disabled={loading}
          >
            <option value="">
              {loading ? "Caricamento…" : "Seleziona una visita"}
            </option>
            {visite.map((v) => (
              <option key={v.id} value={v.id}>
                {v.pazienteNomeCompleto} · {formatDateTime(v.dataOraInizio)} (
                {v.stato})
              </option>
            ))}
          </select>
        </label>

        {selected && (
          <div className="dash-readonly-box">
            <p>
              <strong>Paziente:</strong> {selected.pazienteNomeCompleto}
            </p>
            <p>
              <strong>CF:</strong> {selected.pazienteCodiceFiscale}
            </p>
          </div>
        )}

        <label className="dash-label">
          Titolo referto
          <input
            className="auth-field"
            value={titolo}
            onChange={(e) => setTitolo(e.target.value)}
            required
          />
        </label>

        <label className="dash-label">
          Descrizione (opzionale)
          <textarea
            className="auth-field dash-textarea"
            rows={3}
            value={descrizione}
            onChange={(e) => setDescrizione(e.target.value)}
          />
        </label>

        <label className="dash-label">
          File PDF
          <input
            type="file"
            accept="application/pdf,.pdf"
            className="auth-field"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            required
          />
        </label>

        <button className="dash-btn" type="submit" disabled={submitting}>
          {submitting ? "Upload…" : "Carica referto"}
        </button>
      </form>
    </div>
  );
}
