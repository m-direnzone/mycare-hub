"use client";

import { useState } from "react";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import {
  updateMioProfiloFromDb,
  updateMiaBiografiaMedicoFromDb,
} from "@/src/lib/profilo";
import { toast } from "react-toastify";

export default function ProfiloPage() {
  const { profilo, email, setProfilo } = useDashboard();
  const [nome, setNome] = useState(profilo.nome);
  const [cognome, setCognome] = useState(profilo.cognome);
  const [telefono, setTelefono] = useState(profilo.telefono ?? "");
  const [biografia, setBiografia] = useState(
    profilo.dettagliMedico?.biografia ?? ""
  );
  const [saving, setSaving] = useState(false);

  async function onSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await updateMioProfiloFromDb({
        nome: nome.trim(),
        cognome: cognome.trim(),
        telefono: telefono.trim() || null,
      });

      let dettagliMedico = profilo.dettagliMedico;
      if (profilo.ruolo === "medico" && dettagliMedico) {
        dettagliMedico = await updateMiaBiografiaMedicoFromDb({
          biografia: biografia.trim() || null,
        });
      }

      setProfilo({
        ...profilo,
        ...updated,
        dettagliMedico,
      });
      toast.success("Profilo aggiornato.");
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Aggiornamento non riuscito."
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Profilo personale</h1>
        <p className="dash-page-subtitle">
          Anagrafica e codice fiscale. Il CF non è modificabile.
        </p>
      </header>

      <form className="dash-panel space-y-4" onSubmit={onSave}>
        <div className="dash-form-grid">
          <label className="dash-label">
            Nome
            <input
              className="auth-field"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              required
            />
          </label>
          <label className="dash-label">
            Cognome
            <input
              className="auth-field"
              value={cognome}
              onChange={(e) => setCognome(e.target.value)}
              required
            />
          </label>
          <label className="dash-label">
            Email
            <input className="auth-field" value={email ?? ""} disabled />
          </label>
          <label className="dash-label">
            Codice fiscale
            <input
              className="auth-field"
              value={profilo.codiceFiscale}
              disabled
            />
          </label>
          <label className="dash-label">
            Data di nascita
            <input
              className="auth-field"
              value={profilo.dataNascita}
              disabled
            />
          </label>
          <label className="dash-label">
            Telefono
            <input
              className="auth-field"
              value={telefono}
              onChange={(e) => setTelefono(e.target.value)}
            />
          </label>
          <label className="dash-label">
            Ruolo
            <input className="auth-field" value={profilo.ruolo} disabled />
          </label>
        </div>

        {profilo.ruolo === "medico" && profilo.dettagliMedico && (
          <div className="space-y-4">
            <div className="dash-readonly-box">
              <p>
                <strong>Specializzazione:</strong>{" "}
                {profilo.dettagliMedico.specializzazione}
              </p>
              <p>
                <strong>Albo:</strong>{" "}
                {profilo.dettagliMedico.numeroIscrizioneAlbo}
              </p>
              <p>
                <strong>Tariffa visita:</strong> €{" "}
                {profilo.dettagliMedico.tariffaVisita}
              </p>
            </div>
            <label className="dash-label">
              Biografia
              <textarea
                className="dash-textarea"
                rows={5}
                maxLength={2000}
                value={biografia}
                onChange={(e) => setBiografia(e.target.value)}
                placeholder="Presentati ai pazienti: esperienza, approccio, aree di interesse…"
              />
            </label>
          </div>
        )}

        <button className="dash-btn" type="submit" disabled={saving}>
          {saving ? "Salvataggio…" : "Salva modifiche"}
        </button>
      </form>
    </div>
  );
}
