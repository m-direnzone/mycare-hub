"use client";

import { useEffect, useMemo, useState } from "react";
import {
  FilterableTable,
  type FilterableColumn,
} from "@/src/components/admin/FilterableTable";
import {
  fetchAdminPazienti,
  type AdminPazienteDto,
} from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { toast } from "react-toastify";

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString("it-IT");
  } catch {
    return iso;
  }
}

export default function AdminPazientiPage() {
  const [rows, setRows] = useState<AdminPazienteDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await fetchAdminPazienti();
        if (active) setRows(data);
      } catch (err) {
        if (active) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare i pazienti."
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

  const columns = useMemo<FilterableColumn<AdminPazienteDto>[]>(
    () => [
      { key: "cognome", label: "Cognome", getValue: (r) => r.cognome },
      { key: "nome", label: "Nome", getValue: (r) => r.nome },
      {
        key: "cf",
        label: "Codice fiscale",
        getValue: (r) => r.codiceFiscale,
      },
      {
        key: "nascita",
        label: "Data di nascita",
        getValue: (r) => r.dataNascita,
        render: (r) => formatDate(r.dataNascita),
      },
      {
        key: "telefono",
        label: "Telefono",
        getValue: (r) => r.telefono ?? "",
      },
      {
        key: "creato",
        label: "Registrato il",
        getValue: (r) => r.creatoAt,
        render: (r) => formatDate(r.creatoAt),
      },
    ],
    []
  );

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Pazienti</h1>
        <p className="dash-page-subtitle">
          Elenco in sola lettura dei pazienti registrati sulla piattaforma.
        </p>
      </header>

      {loading ? (
        <p className="text-sm text-muted">Caricamento pazienti…</p>
      ) : (
        <FilterableTable
          columns={columns}
          rows={rows}
          emptyMessage="Nessun paziente registrato."
        />
      )}
    </div>
  );
}
