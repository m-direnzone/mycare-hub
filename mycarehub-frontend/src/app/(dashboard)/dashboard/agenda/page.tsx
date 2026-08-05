"use client";

import { useEffect, useMemo, useState } from "react";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import { ApiError } from "@/src/lib/api";
import {
  buildBulkSlots,
  buildRecurringSlots,
  creaSlotBulkApi,
  eliminaSlotApi,
  fetchMiaAgendaSlotApi,
  filterCreatableSlots,
  formatTime,
  stagioneDateRange,
  toLocalDateKey,
  todayLocalDateKey,
  type FasciaRicorrente,
  type StagionePreset,
} from "@/src/lib/dashboard-api";
import type { SlotDto } from "@/src/types/dto";
import { toast } from "react-toastify";

const WEEKDAYS = ["Lun", "Mar", "Mer", "Gio", "Ven", "Sab", "Dom"];
const WEEKDAY_OPTIONS = [
  { value: 0, label: "Lun" },
  { value: 1, label: "Mar" },
  { value: 2, label: "Mer" },
  { value: 3, label: "Gio" },
  { value: 4, label: "Ven" },
  { value: 5, label: "Sab" },
  { value: 6, label: "Dom" },
];

const STAGIONI: { value: StagionePreset; label: string }[] = [
  { value: "inverno", label: "Inverno" },
  { value: "primavera", label: "Primavera" },
  { value: "estate", label: "Estate" },
  { value: "autunno", label: "Autunno" },
];

type Mode = "singolo" | "ricorrente";

type FasciaForm = {
  id: string;
  weekdays: number[];
  oraInizio: string;
  oraFine: string;
};

function startOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), 1);
}

function addMonths(d: Date, n: number) {
  return new Date(d.getFullYear(), d.getMonth() + n, 1);
}

function monthLabel(d: Date) {
  return new Intl.DateTimeFormat("it-IT", {
    month: "long",
    year: "numeric",
  }).format(d);
}

function buildMonthCells(month: Date) {
  const first = startOfMonth(month);
  const firstWeekday = (first.getDay() + 6) % 7;
  const daysInMonth = new Date(
    month.getFullYear(),
    month.getMonth() + 1,
    0
  ).getDate();

  const cells: ({ kind: "empty" } | { kind: "day"; date: Date; key: string })[] =
    [];

  for (let i = 0; i < firstWeekday; i++) {
    cells.push({ kind: "empty" });
  }

  for (let day = 1; day <= daysInMonth; day++) {
    const date = new Date(month.getFullYear(), month.getMonth(), day);
    cells.push({ kind: "day", date, key: toLocalDateKey(date) });
  }

  return cells;
}

function newFascia(partial?: Partial<FasciaForm>): FasciaForm {
  return {
    id: crypto.randomUUID(),
    weekdays: [1],
    oraInizio: "09:00",
    oraFine: "13:00",
    ...partial,
  };
}

export default function AgendaMedicoPage() {
  const { profilo } = useDashboard();
  const [slots, setSlots] = useState<SlotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [tab, setTab] = useState<"genera" | "agenda">("genera");

  const [mode, setMode] = useState<Mode>("singolo");
  const [viewMonth, setViewMonth] = useState(() => startOfMonth(new Date()));
  const [selectedDay, setSelectedDay] = useState(todayLocalDateKey());
  const [oraInizio, setOraInizio] = useState("09:00");
  const [oraFine, setOraFine] = useState("13:00");
  const [durata, setDurata] = useState(30);

  const [dataInizio, setDataInizio] = useState(todayLocalDateKey());
  const [dataFine, setDataFine] = useState("");
  const [escludiFestivi, setEscludiFestivi] = useState(true);
  const [fasce, setFasce] = useState<FasciaForm[]>([newFascia()]);

  const todayKey = todayLocalDateKey();

  const daysWithSlots = useMemo(() => {
    const map = new Map<string, SlotDto[]>();
    for (const slot of slots) {
      const key = toLocalDateKey(slot.dataOraInizio);
      const list = map.get(key) ?? [];
      list.push(slot);
      map.set(key, list);
    }
    return map;
  }, [slots]);

  const occupiedDays = useMemo(
    () => new Set(daysWithSlots.keys()),
    [daysWithSlots]
  );

  const monthCells = useMemo(() => buildMonthCells(viewMonth), [viewMonth]);

  const canGoPrevMonth = useMemo(() => {
    const current = startOfMonth(new Date());
    return viewMonth.getTime() > current.getTime();
  }, [viewMonth]);

  const daySlots = selectedDay ? (daysWithSlots.get(selectedDay) ?? []) : [];

  const dayStats = useMemo(() => {
    const map = new Map<string, { liberi: number; prenotati: number }>();
    for (const [key, list] of daysWithSlots) {
      let liberi = 0;
      let prenotati = 0;
      for (const s of list) {
        if (s.isPrenotato) prenotati += 1;
        else liberi += 1;
      }
      map.set(key, { liberi, prenotati });
    }
    return map;
  }, [daysWithSlots]);

  const dayLiberi = daySlots.filter((s) => !s.isPrenotato);
  const dayPrenotati = daySlots.filter((s) => s.isPrenotato);

  const previewCount = useMemo(() => {
    const candidati =
      mode === "singolo"
        ? buildBulkSlots({
            data: selectedDay,
            oraInizio,
            oraFine,
            durataMinuti: durata,
          })
        : buildRecurringSlots({
            dataInizio,
            dataFine,
            fasce: fasce.map(
              (f): FasciaRicorrente => ({
                weekdays: f.weekdays,
                oraInizio: f.oraInizio,
                oraFine: f.oraFine,
              })
            ),
            durataMinuti: durata,
            escludiFestivi,
          });
    return filterCreatableSlots(candidati, slots).length;
  }, [
    mode,
    selectedDay,
    oraInizio,
    oraFine,
    durata,
    dataInizio,
    dataFine,
    fasce,
    escludiFestivi,
    slots,
  ]);

  async function load() {
    setLoading(true);
    try {
      const from = new Date();
      from.setMonth(from.getMonth() - 1);
      from.setHours(0, 0, 0, 0);
      const to = new Date();
      to.setMonth(to.getMonth() + 14);
      to.setHours(23, 59, 59, 999);

      const dataSlots = await fetchMiaAgendaSlotApi(
        from.toISOString(),
        to.toISOString()
      );
      setSlots(
        [...dataSlots].sort(
          (a, b) =>
            new Date(a.dataOraInizio).getTime() -
            new Date(b.dataOraInizio).getTime()
        )
      );
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Impossibile caricare l'agenda."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  function onSelectDay(key: string) {
    setSelectedDay(key);
    setViewMonth(startOfMonth(new Date(`${key}T12:00:00`)));
  }

  function applyStagione(stagione: StagionePreset) {
    const range = stagioneDateRange(stagione);
    setDataInizio(range.dataInizio);
    setDataFine(range.dataFine);
  }

  function toggleWeekday(fasciaId: string, day: number) {
    setFasce((prev) =>
      prev.map((f) => {
        if (f.id !== fasciaId) return f;
        const has = f.weekdays.includes(day);
        const weekdays = has
          ? f.weekdays.filter((d) => d !== day)
          : [...f.weekdays, day].sort((a, b) => a - b);
        return { ...f, weekdays };
      })
    );
  }

  async function onGenerate(e: React.FormEvent) {
    e.preventDefault();
    setGenerating(true);
    try {
      if (mode === "singolo" && selectedDay < todayKey) {
        toast.error("Non è possibile creare slot nel passato.");
        return;
      }
      if (mode === "ricorrente") {
        if (!dataInizio || !dataFine) {
          toast.error("Imposta data inizio e data fine del periodo.");
          return;
        }
        if (dataFine < dataInizio) {
          toast.error("La data fine deve essere successiva alla data inizio.");
          return;
        }
        if (dataFine < todayKey) {
          toast.error("Non è possibile creare slot nel passato.");
          return;
        }
        if (fasce.some((f) => f.weekdays.length === 0)) {
          toast.error("Ogni fascia ricorrente deve avere almeno un giorno.");
          return;
        }
      }

      const candidati =
        mode === "singolo"
          ? buildBulkSlots({
              data: selectedDay,
              oraInizio,
              oraFine,
              durataMinuti: durata,
            })
          : buildRecurringSlots({
              dataInizio: dataInizio < todayKey ? todayKey : dataInizio,
              dataFine,
              fasce: fasce.map(
                (f): FasciaRicorrente => ({
                  weekdays: f.weekdays,
                  oraInizio: f.oraInizio,
                  oraFine: f.oraFine,
                })
              ),
              durataMinuti: durata,
              escludiFestivi,
            });

      if (candidati.length === 0) {
        toast.error("Intervallo non valido: nessun slot generabile.");
        return;
      }

      const slotRichiesti = filterCreatableSlots(candidati, slots);
      const skipped = candidati.length - slotRichiesti.length;

      if (slotRichiesti.length === 0) {
        toast.error(
          skipped > 0
            ? "Tutti gli slot richiesti sono già presenti in agenda o nel passato."
            : "Intervallo non valido: nessun slot generabile."
        );
        return;
      }

      const result = await creaSlotBulkApi({
        medicoId: profilo.id,
        slotRichiesti,
      });
      toast.success(
        skipped > 0
          ? `Creati ${result.slotCreati} slot (${skipped} saltati: già presenti o passati).`
          : `Creati ${result.slotCreati} slot.`
      );
      await load();
      setTab("agenda");
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Generazione massiva non riuscita."
      );
    } finally {
      setGenerating(false);
    }
  }

  async function onBlocca(slotId: string) {
    if (!confirm("Bloccare (eliminare) questo slot libero?")) return;
    setBusyId(slotId);
    try {
      await eliminaSlotApi(slotId);
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Blocco slot non riuscito."
      );
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Gestione agenda & slot</h1>
        <p className="dash-page-subtitle">
          Genera slot singoli o ricorrenti e gestisci quelli già in agenda.
        </p>
      </header>

      <div className="dash-tabs" role="tablist" aria-label="Sezioni agenda">
        <button
          type="button"
          role="tab"
          aria-selected={tab === "genera"}
          className={`dash-tab${tab === "genera" ? " is-active" : ""}`}
          onClick={() => setTab("genera")}
        >
          Genera slot
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={tab === "agenda"}
          className={`dash-tab${tab === "agenda" ? " is-active" : ""}`}
          onClick={() => setTab("agenda")}
        >
          Agenda
          {!loading && slots.length > 0 ? ` (${slots.length})` : ""}
        </button>
      </div>

      {tab === "genera" ? (
      <form className="dash-panel space-y-4" onSubmit={onGenerate}>
        <h2 className="dash-section-title">Crea nuovi slot</h2>

        <div className="agenda-mode-toggle" role="group" aria-label="Modalità">
          <button
            type="button"
            className={`agenda-mode-btn${mode === "singolo" ? " is-active" : ""}`}
            onClick={() => setMode("singolo")}
          >
            Giorno singolo
          </button>
          <button
            type="button"
            className={`agenda-mode-btn${mode === "ricorrente" ? " is-active" : ""}`}
            onClick={() => setMode("ricorrente")}
          >
            Ricorrente
          </button>
        </div>

        {mode === "singolo" ? (
          <div className="booking-layout">
            <div className="booking-calendar">
              <div className="booking-cal-header">
                <button
                  type="button"
                  className="booking-cal-nav"
                  disabled={!canGoPrevMonth}
                  onClick={() => setViewMonth((m) => addMonths(m, -1))}
                  aria-label="Mese precedente"
                >
                  ‹
                </button>
                <p className="booking-cal-title">{monthLabel(viewMonth)}</p>
                <button
                  type="button"
                  className="booking-cal-nav"
                  onClick={() => setViewMonth((m) => addMonths(m, 1))}
                  aria-label="Mese successivo"
                >
                  ›
                </button>
              </div>

              <div className="booking-cal-weekdays">
                {WEEKDAYS.map((d) => (
                  <span key={d}>{d}</span>
                ))}
              </div>

              <div className="booking-cal-grid">
                {monthCells.map((cell, idx) => {
                  if (cell.kind === "empty") {
                    return (
                      <span
                        key={`e-${idx}`}
                        className="booking-cal-cell is-empty"
                      />
                    );
                  }

                  const past = cell.key < todayKey;
                  const hasSlots = occupiedDays.has(cell.key);
                  const selected = selectedDay === cell.key;
                  const isToday = cell.key === todayKey;

                  return (
                    <button
                      key={cell.key}
                      type="button"
                      className={[
                        "booking-cal-cell",
                        past ? "is-past" : "",
                        hasSlots ? "is-available has-slots" : "",
                        selected ? "is-selected" : "",
                        isToday ? "is-today" : "",
                      ]
                        .filter(Boolean)
                        .join(" ")}
                      disabled={past}
                      onClick={() => onSelectDay(cell.key)}
                      title={
                        hasSlots
                          ? `${daysWithSlots.get(cell.key)?.length ?? 0} slot già generati`
                          : undefined
                      }
                    >
                      {cell.date.getDate()}
                    </button>
                  );
                })}
              </div>

              <p className="booking-cal-legend">
                I giorni evidenziati hanno già slot in agenda. I giorni passati
                non sono selezionabili.
              </p>
            </div>

            <div className="booking-times">
              <p className="dash-day-label">
                {selectedDay
                  ? new Intl.DateTimeFormat("it-IT", {
                      weekday: "long",
                      day: "numeric",
                      month: "long",
                    }).format(new Date(`${selectedDay}T12:00:00`))
                  : "Seleziona un giorno"}
              </p>

              {daySlots.length > 0 && (
                <div className="agenda-existing-slots">
                  <p className="booking-time-group-label">
                    Già in agenda ({daySlots.length})
                  </p>
                  <div className="dash-slot-grid">
                    {daySlots.map((slot) => (
                      <span
                        key={slot.id}
                        className={`dash-slot is-existing${
                          slot.isPrenotato ? " is-booked" : ""
                        }`}
                        title={slot.isPrenotato ? "Prenotato" : "Libero"}
                      >
                        {formatTime(slot.dataOraInizio)}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              <div className="dash-form-grid">
                <label className="dash-label">
                  Ora inizio
                  <input
                    type="time"
                    className="auth-field"
                    value={oraInizio}
                    onChange={(e) => setOraInizio(e.target.value)}
                    required
                  />
                </label>
                <label className="dash-label">
                  Ora fine
                  <input
                    type="time"
                    className="auth-field"
                    value={oraFine}
                    onChange={(e) => setOraFine(e.target.value)}
                    required
                  />
                </label>
                <label className="dash-label">
                  Durata visita (minuti)
                  <input
                    type="number"
                    min={5}
                    step={5}
                    className="auth-field"
                    value={durata}
                    onChange={(e) => setDurata(Number(e.target.value))}
                    required
                  />
                </label>
              </div>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="agenda-stagioni">
              {STAGIONI.map((s) => (
                <button
                  key={s.value}
                  type="button"
                  className="agenda-stagione-btn"
                  onClick={() => applyStagione(s.value)}
                >
                  {s.label}
                </button>
              ))}
            </div>

            <div className="dash-form-grid">
              <label className="dash-label">
                Data inizio
                <input
                  type="date"
                  className="auth-field"
                  value={dataInizio}
                  min={todayKey}
                  onChange={(e) => setDataInizio(e.target.value)}
                  required
                />
              </label>
              <label className="dash-label">
                Data fine
                <input
                  type="date"
                  className="auth-field"
                  value={dataFine}
                  min={dataInizio || todayKey}
                  onChange={(e) => setDataFine(e.target.value)}
                  required
                />
              </label>
              <label className="dash-label">
                Durata visita (minuti)
                <input
                  type="number"
                  min={5}
                  step={5}
                  className="auth-field"
                  value={durata}
                  onChange={(e) => setDurata(Number(e.target.value))}
                  required
                />
              </label>
            </div>

            <label className="agenda-check">
              <input
                type="checkbox"
                checked={escludiFestivi}
                onChange={(e) => setEscludiFestivi(e.target.checked)}
              />
              Escludi festivi nazionali
            </label>

            <div className="space-y-3">
              <div className="agenda-fasce-header">
                <h3 className="dash-section-title" style={{ margin: 0 }}>
                  Fasce ricorrenti
                </h3>
                <button
                  type="button"
                  className="dash-btn dash-btn-ghost"
                  onClick={() =>
                    setFasce((prev) => [
                      ...prev,
                      newFascia({
                        weekdays: [3],
                        oraInizio: "14:00",
                        oraFine: "18:00",
                      }),
                    ])
                  }
                >
                  + Aggiungi fascia
                </button>
              </div>

              {fasce.map((fascia, idx) => (
                <div key={fascia.id} className="agenda-fascia-card">
                  <div className="agenda-fascia-top">
                    <p className="booking-time-group-label">
                      Fascia {idx + 1}
                    </p>
                    {fasce.length > 1 && (
                      <button
                        type="button"
                        className="dash-btn dash-btn-danger"
                        onClick={() =>
                          setFasce((prev) =>
                            prev.filter((f) => f.id !== fascia.id)
                          )
                        }
                      >
                        Rimuovi
                      </button>
                    )}
                  </div>

                  <div className="agenda-weekday-row">
                    {WEEKDAY_OPTIONS.map((d) => {
                      const active = fascia.weekdays.includes(d.value);
                      return (
                        <button
                          key={d.value}
                          type="button"
                          className={`agenda-weekday-btn${active ? " is-active" : ""}`}
                          onClick={() => toggleWeekday(fascia.id, d.value)}
                          aria-pressed={active}
                        >
                          {d.label}
                        </button>
                      );
                    })}
                  </div>

                  <div className="dash-form-grid">
                    <label className="dash-label">
                      Ora inizio
                      <input
                        type="time"
                        className="auth-field"
                        value={fascia.oraInizio}
                        onChange={(e) =>
                          setFasce((prev) =>
                            prev.map((f) =>
                              f.id === fascia.id
                                ? { ...f, oraInizio: e.target.value }
                                : f
                            )
                          )
                        }
                        required
                      />
                    </label>
                    <label className="dash-label">
                      Ora fine
                      <input
                        type="time"
                        className="auth-field"
                        value={fascia.oraFine}
                        onChange={(e) =>
                          setFasce((prev) =>
                            prev.map((f) =>
                              f.id === fascia.id
                                ? { ...f, oraFine: e.target.value }
                                : f
                            )
                          )
                        }
                        required
                      />
                    </label>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        <p className="text-sm text-muted">
          Anteprima: {previewCount} slot nuovi (sovrapposizioni e passato
          esclusi).
        </p>

        <button
          className="dash-btn"
          type="submit"
          disabled={generating || previewCount === 0}
        >
          {generating ? "Generazione…" : "Genera slot"}
        </button>
      </form>
      ) : (
      <section className="dash-panel space-y-3">
        <div className="agenda-overview-header">
          <h2 className="dash-section-title" style={{ margin: 0 }}>
            La tua agenda
          </h2>
          {!loading && slots.length > 0 && (
            <p className="agenda-overview-meta">
              {slots.length} slot · {dayStats.size} giorni
            </p>
          )}
        </div>

        {loading ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : slots.length === 0 ? (
          <p className="text-sm text-muted">
            Nessuno slot in agenda.{" "}
            <button
              type="button"
              className="agenda-inline-link"
              onClick={() => setTab("genera")}
            >
              Generane alcuni
            </button>
          </p>
        ) : (
          <div className="booking-layout">
            <div className="booking-calendar">
              <div className="booking-cal-header">
                <button
                  type="button"
                  className="booking-cal-nav"
                  onClick={() => setViewMonth((m) => addMonths(m, -1))}
                  aria-label="Mese precedente"
                >
                  ‹
                </button>
                <p className="booking-cal-title">{monthLabel(viewMonth)}</p>
                <button
                  type="button"
                  className="booking-cal-nav"
                  onClick={() => setViewMonth((m) => addMonths(m, 1))}
                  aria-label="Mese successivo"
                >
                  ›
                </button>
              </div>

              <div className="booking-cal-weekdays">
                {WEEKDAYS.map((d) => (
                  <span key={d}>{d}</span>
                ))}
              </div>

              <div className="booking-cal-grid">
                {monthCells.map((cell, idx) => {
                  if (cell.kind === "empty") {
                    return (
                      <span
                        key={`e-${idx}`}
                        className="booking-cal-cell is-empty"
                      />
                    );
                  }

                  const stats = dayStats.get(cell.key);
                  const hasSlots = Boolean(stats);
                  const selected = selectedDay === cell.key;
                  const isToday = cell.key === todayKey;
                  const past = cell.key < todayKey;

                  return (
                    <button
                      key={cell.key}
                      type="button"
                      className={[
                        "booking-cal-cell agenda-overview-cell",
                        past ? "is-past" : "",
                        hasSlots ? "has-slots" : "",
                        stats?.prenotati ? "has-booked" : "",
                        selected ? "is-selected" : "",
                        isToday ? "is-today" : "",
                      ]
                        .filter(Boolean)
                        .join(" ")}
                      onClick={() => onSelectDay(cell.key)}
                      title={
                        stats
                          ? `${stats.liberi} liberi · ${stats.prenotati} prenotati`
                          : "Nessuno slot"
                      }
                    >
                      <span className="agenda-overview-daynum">
                        {cell.date.getDate()}
                      </span>
                      {stats && (
                        <span className="agenda-overview-dots" aria-hidden>
                          {stats.liberi > 0 && (
                            <span className="agenda-dot is-free" />
                          )}
                          {stats.prenotati > 0 && (
                            <span className="agenda-dot is-booked" />
                          )}
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>

              <p className="booking-cal-legend">
                <span className="agenda-legend-item">
                  <span className="agenda-dot is-free" /> Liberi
                </span>
                <span className="agenda-legend-item">
                  <span className="agenda-dot is-booked" /> Prenotati
                </span>
              </p>
            </div>

            <div className="booking-times">
              <p className="dash-day-label">
                {selectedDay
                  ? new Intl.DateTimeFormat("it-IT", {
                      weekday: "long",
                      day: "numeric",
                      month: "long",
                    }).format(new Date(`${selectedDay}T12:00:00`))
                  : "Seleziona un giorno"}
              </p>

              {daySlots.length === 0 ? (
                <p className="text-sm text-muted">
                  Nessuno slot in questo giorno.
                </p>
              ) : (
                <>
                  <div className="agenda-day-summary">
                    <span className="agenda-summary-pill is-free">
                      {dayLiberi.length} liberi
                    </span>
                    <span className="agenda-summary-pill is-booked">
                      {dayPrenotati.length} prenotati
                    </span>
                  </div>

                  {dayLiberi.length > 0 && (
                    <div className="booking-time-group">
                      <p className="booking-time-group-label">Liberi</p>
                      <div className="dash-slot-grid">
                        {dayLiberi.map((slot) => (
                          <button
                            key={slot.id}
                            type="button"
                            className="dash-slot agenda-slot-chip"
                            disabled={busyId === slot.id}
                            onClick={() => void onBlocca(slot.id)}
                            title="Blocca questo slot"
                          >
                            {formatTime(slot.dataOraInizio)}
                            <span className="agenda-slot-action">
                              {busyId === slot.id ? "…" : "Blocca"}
                            </span>
                          </button>
                        ))}
                      </div>
                    </div>
                  )}

                  {dayPrenotati.length > 0 && (
                    <div className="booking-time-group">
                      <p className="booking-time-group-label">Prenotati</p>
                      <div className="dash-slot-grid">
                        {dayPrenotati.map((slot) => (
                          <span
                            key={slot.id}
                            className="dash-slot is-existing is-booked"
                            title={`${formatTime(slot.dataOraInizio)} – ${formatTime(slot.dataOraFine)}`}
                          >
                            {formatTime(slot.dataOraInizio)}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        )}
      </section>
      )}
    </div>
  );
}
