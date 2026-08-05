"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createSpecializzazione,
  deleteSpecializzazione,
  fetchSpecializzazioniCatalogo,
  type SpecializzazioneDto,
} from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { toast } from "react-toastify";

export default function AdminSpecializzazioniPage() {
  const [rows, setRows] = useState<SpecializzazioneDto[]>([]);
  const [nome, setNome] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  async function load() {
    setLoading(true);
    try {
      setRows(await fetchSpecializzazioniCatalogo());
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Impossibile caricare le specializzazioni."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onAdd(e: FormEvent) {
    e.preventDefault();
    const trimmed = nome.trim();
    if (!trimmed) return;
    setSaving(true);
    try {
      await createSpecializzazione(trimmed);
      setNome("");
      toast.success("Specializzazione aggiunta.");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Inserimento non riuscito."
      );
    } finally {
      setSaving(false);
    }
  }

  async function onDelete(row: SpecializzazioneDto) {
    if (
      !confirm(
        `Rimuovere «${row.nome}» dal catalogo? Lo storico verrà conservato.`
      )
    ) {
      return;
    }
    try {
      await deleteSpecializzazione(row.id);
      toast.success("Specializzazione disabilitata (soft delete).");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Rimozione non riuscita."
      );
    }
  }

  const sorted = [...rows].sort((a, b) =>
    a.nome.localeCompare(b.nome, "it", { sensitivity: "base" })
  );
  const columns = 4;
  const rowCount = Math.max(1, Math.ceil(sorted.length / columns));

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Catalogo specializzazioni</h1>
        <p className="dash-page-subtitle">
          Gestisci le voci disponibili nelle select di prenotazione e
          onboarding medici. Le rimozioni sono soft delete e non sono
          consentite se la specializzazione è assegnata a un medico attivo.
        </p>
      </header>

      <section className="dash-panel mb-5 space-y-3">
        <div className="border-b border-[var(--border)] pb-3">
          <h2 className="dash-section-title">Aggiungi voce</h2>
          <p className="mt-1 text-sm text-[var(--muted)]">
            Inserisci una nuova specializzazione nel catalogo
          </p>
        </div>
        <form
          className="flex flex-col gap-3 sm:flex-row sm:items-end"
          onSubmit={onAdd}
        >
          <label className="dash-label min-w-0 flex-1">
            Nome specializzazione
            <input
              className="auth-field"
              value={nome}
              maxLength={150}
              placeholder="Es. Cardiologia"
              onChange={(e) => setNome(e.target.value)}
            />
          </label>
          <button
            className="dash-btn shrink-0 sm:min-w-[8.5rem]"
            type="submit"
            disabled={saving || !nome.trim()}
          >
            {saving ? "Salvataggio…" : "Aggiungi"}
          </button>
        </form>
      </section>

      {loading ? (
        <p className="text-sm text-muted">Caricamento catalogo…</p>
      ) : (
        <section className="dash-panel space-y-4">
          <div className="flex flex-wrap items-end justify-between gap-2 border-b border-[var(--border)] pb-3">
            <div>
              <h2 className="dash-section-title">Specializzazioni attive</h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Voci attualmente disponibili nel sistema
              </p>
            </div>
            <span className="rounded-lg bg-[var(--brand-soft)] px-2.5 py-1 text-xs font-semibold text-[var(--brand-deep)]">
              {rows.length} {rows.length === 1 ? "voce" : "voci"}
            </span>
          </div>

          {rows.length === 0 ? (
            <p className="dash-empty py-6 text-center">
              Nessuna specializzazione.
            </p>
          ) : (
            <ul
              className="m-0 grid list-none gap-3 p-0"
              style={{
                gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))`,
                gridTemplateRows: `repeat(${rowCount}, auto)`,
                gridAutoFlow: "column",
              }}
            >
              {sorted.map((row) => (
                <li
                  key={row.id}
                  className="group flex items-center justify-between gap-3 rounded-xl border border-[var(--border)] bg-gradient-to-br from-white/90 to-[var(--brand-soft)]/25 px-4 py-3.5 transition-[border-color,box-shadow] hover:border-[var(--brand)]/35 hover:shadow-[0_8px_24px_rgba(15,107,92,0.08)]"
                >
                  <span className="truncate font-medium text-[var(--foreground)]">
                    {row.nome}
                  </span>
                  <button
                    type="button"
                    className="dash-btn dash-btn-danger shrink-0 !p-2 opacity-80 transition-opacity group-hover:opacity-100"
                    aria-label={`Rimuovi ${row.nome}`}
                    title="Rimuovi"
                    onClick={() => void onDelete(row)}
                  >
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      className="h-4 w-4"
                      aria-hidden
                    >
                      <path d="M3 6h18" />
                      <path d="M8 6V4h8v2" />
                      <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" />
                      <path d="M10 11v6" />
                      <path d="M14 11v6" />
                    </svg>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  );
}
