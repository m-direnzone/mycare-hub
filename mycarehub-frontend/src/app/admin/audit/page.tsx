"use client";

import { useEffect, useMemo, useState } from "react";
import {
  FilterableTable,
  type FilterableColumn,
} from "@/src/components/admin/FilterableTable";
import { fetchAdminAudit, type AuditLogDto } from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { toast } from "react-toastify";

function formatDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString("it-IT", {
      dateStyle: "short",
      timeStyle: "medium",
    });
  } catch {
    return iso;
  }
}

export default function AdminAuditPage() {
  const [rows, setRows] = useState<AuditLogDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await fetchAdminAudit(300);
        if (active) setRows(data);
      } catch (err) {
        if (active) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare il log attività."
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

  const columns = useMemo<FilterableColumn<AuditLogDto>[]>(
    () => [
      {
        key: "data",
        label: "Data",
        getValue: (r) => formatDateTime(r.creatoAt),
      },
      {
        key: "attore",
        label: "Attore",
        getValue: (r) => r.attoreNome ?? "",
      },
      {
        key: "azione",
        label: "Azione",
        getValue: (r) => r.azione,
      },
      {
        key: "entita",
        label: "Entità",
        getValue: (r) => r.entitaTipo ?? "",
      },
      {
        key: "dettagli",
        label: "Dettagli",
        getValue: (r) => r.dettagli ?? "",
      },
    ],
    []
  );

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Log delle attività</h1>
        <p className="dash-page-subtitle">
          Audit trail delle azioni critiche eseguite dagli amministratori.
        </p>
      </header>

      {loading ? (
        <p className="text-sm text-muted">Caricamento log…</p>
      ) : (
        <FilterableTable
          columns={columns}
          rows={rows}
          emptyMessage="Nessuna attività registrata."
        />
      )}
    </div>
  );
}
