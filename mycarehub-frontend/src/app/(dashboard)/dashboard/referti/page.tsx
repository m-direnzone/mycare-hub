"use client";

import { useEffect, useMemo, useState } from "react";
import { ApiError } from "@/src/lib/api";
import {
  caricaRefertoApi,
  fetchAgendaMedicoApi,
  formatDateTime,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto } from "@/src/types/dto";
import { toast } from "react-toastify";

export default function RefertiMedicoPage() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [prenotazioneId, setPrenotazioneId] = useState("");
  const [titolo, setTitolo] = useState("");
  const [descrizione, setDescrizione] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

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
          toast.error(
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
      toast.error("Seleziona una visita e un file PDF.");
      return;
    }
    if (!file.name.toLowerCase().endsWith(".pdf")) {
      toast.error("Sono accettati solo file PDF.");
      return;
    }

    setSubmitting(true);
    try {
      const form = new FormData();
      form.append("prenotazioneId", selected.id);
      form.append("pazienteId", selected.pazienteId);
      form.append("titolo", titolo.trim());
      if (descrizione.trim()) form.append("descrizione", descrizione.trim());
      form.append("file", file);

      await caricaRefertoApi(form);
      toast.success("Referto caricato correttamente.");
      setTitolo("");
      setDescrizione("");
      setFile(null);
      setPrenotazioneId("");
    } catch (err) {
      toast.error(
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
