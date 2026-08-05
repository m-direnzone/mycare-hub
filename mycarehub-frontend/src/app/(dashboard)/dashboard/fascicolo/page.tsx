"use client";

import { useEffect, useMemo, useState } from "react";
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

  const gruppi = useMemo(() => {
    const bySpec = new Map<string, RefertoDto[]>();
    for (const r of referti) {
      const key = r.medicoSpecializzazione?.trim() || "Altro";
      const list = bySpec.get(key);
      if (list) list.push(r);
      else bySpec.set(key, [r]);
    }
    return [...bySpec.entries()]
      .map(([specializzazione, items]) => ({
        specializzazione,
        items: [...items].sort(
          (a, b) =>
            new Date(b.dataEmissione).getTime() -
            new Date(a.dataEmissione).getTime()
        ),
      }))
      .sort(
        (a, b) =>
          new Date(b.items[0].dataEmissione).getTime() -
          new Date(a.items[0].dataEmissione).getTime()
      );
  }, [referti]);

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
        <div className="dash-fascicolo-groups">
          {gruppi.map(({ specializzazione, items }) => (
            <section key={specializzazione} className="dash-fascicolo-group">
              <h2 className="dash-section-title">{specializzazione}</h2>
              <ul className="dash-list">
                {items.map((r) => (
                  <li key={r.id} className="dash-list-item">
                    <div className="dash-list-main">
                      <p className="dash-list-title">{r.titolo}</p>
                      <p className="dash-list-meta">
                        {formatDate(r.dataEmissione)} · {r.medicoNomeCompleto}
                      </p>
                      {r.descrizione && (
                        <p className="dash-list-note">{r.descrizione}</p>
                      )}
                    </div>
                    <div className="dash-list-actions">
                      <a
                        className="dash-btn dash-btn-nowrap"
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
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
