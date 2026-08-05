"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  FilterableTable,
  type FilterableColumn,
} from "@/src/components/admin/FilterableTable";
import {
  fetchAdminMedici,
  riattivaAdminMedico,
  softDeleteAdminMedico,
  updateAdminMedico,
  type AdminMedicoDto,
  type AdminMedicoUpdateDto,
} from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { fetchSpecializzazioniNomi } from "@/src/lib/specializzazioni";
import { toast } from "react-toastify";

const EMPTY_EDIT: AdminMedicoUpdateDto = {
  nome: "",
  cognome: "",
  telefono: "",
  specializzazione: "",
  numeroIscrizioneAlbo: "",
  biografia: "",
  tariffaVisita: 80,
  attivo: true,
};

export default function AdminMediciPage() {
  const [rows, setRows] = useState<AdminMedicoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<AdminMedicoDto | null>(null);
  const [form, setForm] = useState<AdminMedicoUpdateDto>(EMPTY_EDIT);
  const [saving, setSaving] = useState(false);
  const [specializzazioni, setSpecializzazioni] = useState<string[]>([]);

  async function load() {
    setLoading(true);
    try {
      const [medici, specs] = await Promise.all([
        fetchAdminMedici(),
        fetchSpecializzazioniNomi(),
      ]);
      setRows(medici);
      setSpecializzazioni(specs);
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Impossibile caricare i medici."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const columns = useMemo<FilterableColumn<AdminMedicoDto>[]>(
    () => [
      {
        key: "cognome",
        label: "Cognome",
        getValue: (r) => r.cognome,
      },
      {
        key: "nome",
        label: "Nome",
        getValue: (r) => r.nome,
      },
      {
        key: "email",
        label: "Email",
        getValue: (r) => r.email ?? "",
      },
      {
        key: "cf",
        label: "Codice fiscale",
        getValue: (r) => r.codiceFiscale,
      },
      {
        key: "spec",
        label: "Specializzazione",
        getValue: (r) => r.specializzazione,
      },
      {
        key: "albo",
        label: "Albo",
        getValue: (r) => r.numeroIscrizioneAlbo,
      },
      {
        key: "tariffa",
        label: "Tariffa",
        getValue: (r) => String(r.tariffaVisita),
        render: (r) => `${r.tariffaVisita.toFixed(2)} €`,
      },
      {
        key: "stato",
        label: "Stato",
        getValue: (r) =>
          !r.attivo
            ? "disabilitato"
            : r.passwordCambiata
              ? "attivo"
              : "in attesa",
        render: (r) => (
          <span
            className={`dash-badge ${
              !r.attivo
                ? "dash-badge-cancellata"
                : r.passwordCambiata
                  ? "dash-badge-completata"
                  : "dash-badge-in_attesa"
            }`}
          >
            {!r.attivo
              ? "Disabilitato"
              : r.passwordCambiata
                ? "Attivo"
                : "In attesa"}
          </span>
        ),
      },
    ],
    []
  );

  function openEdit(row: AdminMedicoDto) {
    setEditing(row);
    setForm({
      nome: row.nome,
      cognome: row.cognome,
      telefono: row.telefono ?? "",
      specializzazione: row.specializzazione,
      numeroIscrizioneAlbo: row.numeroIscrizioneAlbo,
      biografia: row.biografia ?? "",
      tariffaVisita: row.tariffaVisita,
      attivo: row.attivo,
    });
  }

  async function onSave(e: FormEvent) {
    e.preventDefault();
    if (!editing) return;
    setSaving(true);
    try {
      await updateAdminMedico(editing.id, {
        ...form,
        telefono: form.telefono?.trim() || null,
        biografia: form.biografia?.trim() || null,
      });
      toast.success("Medico aggiornato.");
      setEditing(null);
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Salvataggio non riuscito."
      );
    } finally {
      setSaving(false);
    }
  }

  async function onSoftDelete(row: AdminMedicoDto) {
    if (
      !confirm(
        `Disabilitare il medico ${row.nome} ${row.cognome}? Lo storico verrà conservato.`
      )
    ) {
      return;
    }
    try {
      await softDeleteAdminMedico(row.id);
      toast.success("Medico disabilitato (soft delete).");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Operazione non riuscita."
      );
    }
  }

  async function onRiattiva(row: AdminMedicoDto) {
    try {
      await riattivaAdminMedico(row.id);
      toast.success("Medico riattivato.");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Operazione non riuscita."
      );
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header dash-page-header-row">
        <div>
          <h1 className="dash-page-title">Gestione medici</h1>
          <p className="dash-page-subtitle">
            Modifica anagrafica e dettagli, oppure disabilita senza perdere lo
            storico. Non è possibile disabilitare un medico con prenotazioni
            confermate in futuro.
          </p>
        </div>
        <Link href="/admin/medici/nuovo" className="dash-btn">
          Nuovo medico
        </Link>
      </header>

      {loading ? (
        <p className="text-sm text-muted">Caricamento medici…</p>
      ) : (
        <FilterableTable
          columns={columns}
          rows={rows}
          emptyMessage="Nessun medico trovato."
          actions={(row) => (
            <div className="admin-row-actions">
              <button
                type="button"
                className="dash-btn dash-btn-ghost"
                onClick={() => openEdit(row)}
              >
                Modifica
              </button>
              {row.attivo ? (
                <button
                  type="button"
                  className="dash-btn dash-btn-danger"
                  onClick={() => void onSoftDelete(row)}
                >
                  Rimuovi
                </button>
              ) : (
                <button
                  type="button"
                  className="dash-btn"
                  onClick={() => void onRiattiva(row)}
                >
                  Riattiva
                </button>
              )}
            </div>
          )}
        />
      )}

      {editing ? (
        <div className="admin-modal-backdrop" role="presentation">
          <div className="admin-modal" role="dialog" aria-modal="true">
            <header className="admin-modal-header">
              <h2>Modifica medico</h2>
              <button
                type="button"
                className="dash-btn dash-btn-ghost"
                onClick={() => setEditing(null)}
              >
                Chiudi
              </button>
            </header>
            <form className="dash-form-grid" onSubmit={onSave}>
              <label className="dash-label">
                Nome
                <input
                  className="auth-field"
                  required
                  value={form.nome}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, nome: e.target.value }))
                  }
                />
              </label>
              <label className="dash-label">
                Cognome
                <input
                  className="auth-field"
                  required
                  value={form.cognome}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, cognome: e.target.value }))
                  }
                />
              </label>
              <label className="dash-label">
                Telefono
                <input
                  className="auth-field"
                  value={form.telefono ?? ""}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, telefono: e.target.value }))
                  }
                />
              </label>
              <label className="dash-label">
                Specializzazione
                <input
                  className="auth-field"
                  list="admin-spec-list"
                  required
                  value={form.specializzazione}
                  onChange={(e) =>
                    setForm((p) => ({
                      ...p,
                      specializzazione: e.target.value,
                    }))
                  }
                />
                <datalist id="admin-spec-list">
                  {specializzazioni.map((s) => (
                    <option key={s} value={s} />
                  ))}
                </datalist>
              </label>
              <label className="dash-label">
                N. iscrizione albo
                <input
                  className="auth-field"
                  required
                  value={form.numeroIscrizioneAlbo}
                  onChange={(e) =>
                    setForm((p) => ({
                      ...p,
                      numeroIscrizioneAlbo: e.target.value,
                    }))
                  }
                />
              </label>
              <label className="dash-label">
                Tariffa visita (€)
                <input
                  className="auth-field"
                  type="number"
                  min={0.01}
                  max={10000}
                  step={0.01}
                  required
                  value={form.tariffaVisita}
                  onChange={(e) =>
                    setForm((p) => ({
                      ...p,
                      tariffaVisita: Number(e.target.value),
                    }))
                  }
                />
              </label>
              <label className="dash-label" style={{ gridColumn: "1 / -1" }}>
                Biografia
                <textarea
                  className="auth-field dash-textarea"
                  value={form.biografia ?? ""}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, biografia: e.target.value }))
                  }
                />
              </label>
              <label className="dash-label admin-checkbox">
                <input
                  type="checkbox"
                  checked={form.attivo}
                  onChange={(e) =>
                    setForm((p) => ({ ...p, attivo: e.target.checked }))
                  }
                />
                Medico attivo
              </label>
              <div className="admin-modal-actions">
                <button
                  type="button"
                  className="dash-btn dash-btn-ghost"
                  onClick={() => setEditing(null)}
                >
                  Annulla
                </button>
                <button className="dash-btn" type="submit" disabled={saving}>
                  {saving ? "Salvataggio…" : "Salva"}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}
    </div>
  );
}
