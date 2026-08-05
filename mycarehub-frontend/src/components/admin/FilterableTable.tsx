"use client";

import { useMemo, useState, type ReactNode } from "react";

export type FilterableColumn<T> = {
  key: string;
  label: string;
  getValue: (row: T) => string;
  render?: (row: T) => ReactNode;
  className?: string;
};

export function FilterableTable<T extends { id: string }>({
  columns,
  rows,
  emptyMessage = "Nessun risultato.",
  actions,
}: {
  columns: FilterableColumn<T>[];
  rows: T[];
  emptyMessage?: string;
  actions?: (row: T) => ReactNode;
}) {
  const [showFilters, setShowFilters] = useState(false);
  const [filters, setFilters] = useState<Record<string, string>>({});

  const filtered = useMemo(() => {
    return rows.filter((row) =>
      columns.every((col) => {
        const needle = (filters[col.key] ?? "").trim().toLowerCase();
        if (!needle) return true;
        return col.getValue(row).toLowerCase().includes(needle);
      })
    );
  }, [rows, columns, filters]);

  return (
    <div className="admin-table-wrap">
      <div className="admin-table-toolbar">
        <p className="admin-table-count">
          {filtered.length} di {rows.length}
        </p>
        <button
          type="button"
          className="dash-btn dash-btn-ghost"
          onClick={() => setShowFilters((v) => !v)}
        >
          {showFilters ? "Nascondi filtri" : "Mostra filtri"}
        </button>
      </div>

      <div className="admin-table-scroll">
        <table className="admin-table">
          <thead>
            <tr>
              {columns.map((col) => (
                <th key={col.key} className={col.className}>
                  {col.label}
                </th>
              ))}
              {actions ? <th>Azioni</th> : null}
            </tr>
            {showFilters ? (
              <tr className="admin-table-filters">
                {columns.map((col) => (
                  <th key={`f-${col.key}`}>
                    <input
                      className="admin-filter-input"
                      placeholder={`Filtra ${col.label.toLowerCase()}…`}
                      value={filters[col.key] ?? ""}
                      onChange={(e) =>
                        setFilters((prev) => ({
                          ...prev,
                          [col.key]: e.target.value,
                        }))
                      }
                    />
                  </th>
                ))}
                {actions ? <th /> : null}
              </tr>
            ) : null}
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length + (actions ? 1 : 0)}
                  className="admin-table-empty"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              filtered.map((row) => (
                <tr key={row.id}>
                  {columns.map((col) => (
                    <td key={col.key} className={col.className}>
                      {col.render ? col.render(row) : col.getValue(row)}
                    </td>
                  ))}
                  {actions ? (
                    <td className="admin-table-actions">{actions(row)}</td>
                  ) : null}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
