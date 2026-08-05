"use client";

import { useEffect, useMemo, useState } from "react";
import {
  FilterableTable,
  type FilterableColumn,
} from "@/src/components/admin/FilterableTable";
import {
  fetchAdminPrenotazioni,
  formatEuro,
  type AdminPrenotazioneDto,
} from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { statoLabel } from "@/src/lib/dashboard-api";
import { toast } from "react-toastify";

function formatDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString("it-IT", {
      dateStyle: "short",
      timeStyle: "short",
    });
  } catch {
    return iso;
  }
}

export default function AdminPrenotazioniPage() {
  const now = new Date();
  const [anno, setAnno] = useState(now.getFullYear());
  const [mese, setMese] = useState(now.getMonth() + 1);
  const [giorno, setGiorno] = useState<number | "">("");
  const [rows, setRows] = useState<AdminPrenotazioneDto[]>([]);
  const [loading, setLoading] = useState(true);

  async function load(params: {
    anno: number;
    mese: number;
    giorno?: number | "";
  }) {
    setLoading(true);
    try {
      const data = await fetchAdminPrenotazioni({
        anno: params.anno,
        mese: params.mese,
        giorno: params.giorno === "" ? undefined : params.giorno,
      });
      setRows(data);
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
    void load({ anno, mese, giorno });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const columns = useMemo<FilterableColumn<AdminPrenotazioneDto>[]>(
    () => [
      {
        key: "data",
        label: "Data/ora",
        getValue: (r) => formatDateTime(r.dataOraInizio),
      },
      {
        key: "stato",
        label: "Stato",
        getValue: (r) => String(r.stato),
        render: (r) => (
          <span className={`dash-badge dash-badge-${r.stato}`}>
            {statoLabel(String(r.stato))}
          </span>
        ),
      },
      {
        key: "medico",
        label: "Medico",
        getValue: (r) => r.medicoNomeCompleto,
      },
      {
        key: "spec",
        label: "Specializzazione",
        getValue: (r) => r.medicoSpecializzazione,
      },
      {
        key: "paziente",
        label: "Paziente",
        getValue: (r) => r.pazienteNomeCompleto,
      },
      {
        key: "cf",
        label: "CF paziente",
        getValue: (r) => r.pazienteCodiceFiscale,
      },
      {
        key: "tariffa",
        label: "Tariffa",
        getValue: (r) => String(r.tariffaVisita ?? ""),
        render: (r) =>
          r.tariffaVisita != null ? formatEuro(r.tariffaVisita) : "—",
      },
    ],
    []
  );

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Prenotazioni globali</h1>
        <p className="dash-page-subtitle">
          Monitoraggio flussi clinici per giorno o mese.
        </p>
      </header>

      <form
        className="admin-period-filters"
        onSubmit={(e) => {
          e.preventDefault();
          void load({ anno, mese, giorno });
        }}
      >
        <label className="dash-label">
          Anno
          <input
            className="auth-field"
            type="number"
            min={2020}
            max={2100}
            value={anno}
            onChange={(e) => setAnno(Number(e.target.value))}
          />
        </label>
        <label className="dash-label">
          Mese
          <select
            className="auth-field"
            value={mese}
            onChange={(e) => setMese(Number(e.target.value))}
          >
            {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
              <option key={m} value={m}>
                {m.toString().padStart(2, "0")}
              </option>
            ))}
          </select>
        </label>
        <label className="dash-label">
          Giorno (opzionale)
          <input
            className="auth-field"
            type="number"
            min={1}
            max={31}
            value={giorno}
            placeholder="Tutto il mese"
            onChange={(e) =>
              setGiorno(e.target.value === "" ? "" : Number(e.target.value))
            }
          />
        </label>
        <button type="submit" className="dash-btn">
          Applica
        </button>
      </form>

      {loading ? (
        <p className="text-sm text-muted">Caricamento prenotazioni…</p>
      ) : (
        <FilterableTable
          columns={columns}
          rows={rows}
          emptyMessage="Nessuna prenotazione nel periodo selezionato."
        />
      )}
    </div>
  );
}
