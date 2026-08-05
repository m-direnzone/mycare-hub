"use client";

import { useEffect, useState } from "react";
import { ApiError } from "@/src/lib/api";
import {
  fetchFascicoloApi,
  formatDate,
} from "@/src/lib/dashboard-api";
import type { RefertoDto } from "@/src/types/dto";
import { toast } from "react-toastify";

export default function FascicoloPage() {
  const [referti, setReferti] = useState<RefertoDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await fetchFascicoloApi();
        if (active) {
          setReferti(
            [...data].sort(
              (a, b) =>
                new Date(b.dataEmissione).getTime() -
                new Date(a.dataEmissione).getTime()
            )
          );
        }
      } catch (err) {
        if (active) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare il fascicolo."
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

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Fascicolo sanitario</h1>
        <p className="dash-page-subtitle">
          Storico referti clinici con download protetto via Signed URL.
        </p>
      </header>

      {loading ? (
        <p className="text-sm text-muted">Caricamento…</p>
      ) : referti.length === 0 ? (
        <div className="dash-empty">
          <p>Nessun referto presente nel fascicolo.</p>
        </div>
      ) : (
        <ul className="dash-list">
          {referti.map((r) => (
            <li key={r.id} className="dash-list-item">
              <div className="dash-list-main">
                <p className="dash-list-title">{r.titolo}</p>
                <p className="dash-list-meta">
                  {formatDate(r.dataEmissione)} · {r.medicoNomeCompleto} (
                  {r.medicoSpecializzazione})
                </p>
                {r.descrizione && (
                  <p className="dash-list-note">{r.descrizione}</p>
                )}
              </div>
              <div className="dash-list-actions">
                <a
                  className="dash-btn"
                  href={r.downloadUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Scarica PDF
                </a>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
