"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "react-toastify";
import { FancySelect } from "@/src/components/dashboard/FancySelect";
import { ApiError } from "@/src/lib/api";
import {
  cercaMediciApi,
  creaPrenotazioneApi,
  EMPTY_SLOT_ID,
  fetchSlotDisponibiliApi,
  formatTime,
  todayLocalDateKey,
  toLocalDateKey,
} from "@/src/lib/dashboard-api";
import { fetchSpecializzazioniNomi } from "@/src/lib/specializzazioni";
import type { ProfiloDto, SlotDto } from "@/src/types/dto";

const WEEKDAYS = ["Lun", "Mar", "Mer", "Gio", "Ven", "Sab", "Dom"];

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
  const firstWeekday = (first.getDay() + 6) % 7; // Monday-first
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

export default function NuovaPrenotazionePage() {
  const router = useRouter();
  const [specializzazione, setSpecializzazione] = useState("");
  const [allMedici, setAllMedici] = useState<ProfiloDto[]>([]);
  const [selectedMedicoId, setSelectedMedicoId] = useState("");
  const [selectedMedico, setSelectedMedico] = useState<ProfiloDto | null>(null);
  const [slots, setSlots] = useState<SlotDto[]>([]);
  const [selectedSlotKey, setSelectedSlotKey] = useState<string | null>(null);
  const [notePaziente, setNotePaziente] = useState("");
  const [selectedDay, setSelectedDay] = useState("");
  const [viewMonth, setViewMonth] = useState(() => startOfMonth(new Date()));
  const [loadingMedici, setLoadingMedici] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [specializzazioni, setSpecializzazioni] = useState<string[]>([]);

  const todayKey = todayLocalDateKey();

  useEffect(() => {
    let active = true;
    (async () => {
      const list = await fetchSpecializzazioniNomi();
      if (active) setSpecializzazioni(list);
    })();
    return () => {
      active = false;
    };
  }, []);

  const mediciFiltrati = useMemo(() => {
    if (!specializzazione) return allMedici;
    return allMedici.filter(
      (m) => m.dettagliMedico?.specializzazione === specializzazione
    );
  }, [allMedici, specializzazione]);

  const nessunMedicoDisponibile =
    !loadingMedici && mediciFiltrati.length === 0;

  const specializzazioneOptions = useMemo(() => {
    const base = [
      { value: "", label: "Tutte le specializzazioni" },
      ...specializzazioni.map((item) => ({ value: item, label: item })),
    ];
    if (specializzazione && !specializzazioni.includes(specializzazione)) {
      base.push({ value: specializzazione, label: specializzazione });
    }
    return base;
  }, [specializzazione, specializzazioni]);

  const medicoOptions = useMemo(() => {
    if (loadingMedici) {
      return [{ value: "", label: "Caricamento medici…" }];
    }
    if (nessunMedicoDisponibile) {
      return [{ value: "", label: "Nessun medico disponibile" }];
    }
    return mediciFiltrati.map((m) => ({
      value: m.id,
      label: `${m.nome} ${m.cognome}`,
      hint: m.dettagliMedico?.specializzazione
        ? `€ ${m.dettagliMedico.tariffaVisita} · ${m.dettagliMedico.specializzazione}`
        : undefined,
    }));
  }, [loadingMedici, nessunMedicoDisponibile, mediciFiltrati]);

  const slotsByDay = useMemo(() => {
    const map = new Map<string, SlotDto[]>();
    for (const slot of slots) {
      const key = toLocalDateKey(slot.dataOraInizio);
      const list = map.get(key) ?? [];
      list.push(slot);
      map.set(key, list);
    }
    for (const list of map.values()) {
      list.sort(
        (a, b) =>
          new Date(a.dataOraInizio).getTime() -
          new Date(b.dataOraInizio).getTime()
      );
    }
    return map;
  }, [slots]);

  const availableDays = useMemo(
    () => new Set(slotsByDay.keys()),
    [slotsByDay]
  );

  const daySlots = selectedDay ? (slotsByDay.get(selectedDay) ?? []) : [];

  const selectedSlot = useMemo(() => {
    if (!selectedSlotKey) return null;
    return slots.find((s) => s.dataOraInizio === selectedSlotKey) ?? null;
  }, [selectedSlotKey, slots]);

  const monthCells = useMemo(() => buildMonthCells(viewMonth), [viewMonth]);

  const canGoPrevMonth = useMemo(() => {
    const current = startOfMonth(new Date());
    return viewMonth.getTime() > current.getTime();
  }, [viewMonth]);

  useEffect(() => {
    let active = true;
    (async () => {
      setLoadingMedici(true);
      try {
        const results = await cercaMediciApi({});
        if (active) setAllMedici(Array.isArray(results) ? results : []);
      } catch (err) {
        if (active) {
          setAllMedici([]);
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare i medici."
          );
        }
      } finally {
        if (active) setLoadingMedici(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  function onSpecializzazioneChange(value: string) {
    setSpecializzazione(value);
    if (selectedMedicoId) {
      const medico = allMedici.find((m) => m.id === selectedMedicoId);
      if (
        value &&
        medico &&
        medico.dettagliMedico?.specializzazione !== value
      ) {
        setSelectedMedicoId("");
        setSelectedMedico(null);
        setSlots([]);
        setSelectedSlotKey(null);
        setSelectedDay("");
      }
    }
  }

  function onMedicoChange(medicoId: string) {
    setSelectedMedicoId(medicoId);
    setSelectedSlotKey(null);
    setSelectedDay("");
    if (!medicoId) {
      setSelectedMedico(null);
      setSlots([]);
      return;
    }
    const medico = allMedici.find((m) => m.id === medicoId) ?? null;
    setSelectedMedico(medico);
    if (medico?.dettagliMedico?.specializzazione) {
      setSpecializzazione(medico.dettagliMedico.specializzazione);
    }
  }

  useEffect(() => {
    if (!selectedMedico) return;
    let active = true;

    (async () => {
      setLoadingSlots(true);
      setSelectedSlotKey(null);
      try {
        const dataInizio = new Date();
        dataInizio.setHours(0, 0, 0, 0);
        const dataFine = new Date(dataInizio);
        dataFine.setDate(dataFine.getDate() + 60);
        dataFine.setHours(23, 59, 59, 999);

        const data = await fetchSlotDisponibiliApi(
          selectedMedico.id,
          dataInizio.toISOString(),
          dataFine.toISOString()
        );
        if (!active) return;
        // Solo slot persistiti dall'agenda del medico (niente fasce virtuali).
        setSlots(
          Array.isArray(data)
            ? data.filter(
                (s) =>
                  !s.isPrenotato &&
                  s.id &&
                  s.id !== EMPTY_SLOT_ID &&
                  s.id !== ""
              )
            : []
        );
      } catch (err) {
        if (active) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare gli slot."
          );
          setSlots([]);
        }
      } finally {
        if (active) setLoadingSlots(false);
      }
    })();

    return () => {
      active = false;
    };
  }, [selectedMedico]);

  useEffect(() => {
    if (!selectedDay) return;
    if (!availableDays.has(selectedDay)) {
      setSelectedDay("");
      setSelectedSlotKey(null);
    }
  }, [availableDays, selectedDay]);

  function onSelectDay(key: string) {
    if (key < todayKey) return;
    if (!availableDays.has(key)) return;
    setSelectedDay(key);
    setSelectedSlotKey(null);
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedMedico || !selectedSlot) {
      toast.error("Seleziona un giorno e un orario disponibile.");
      return;
    }

    setSubmitting(true);
    try {
      const hasRealSlot =
        selectedSlot.id &&
        selectedSlot.id !== EMPTY_SLOT_ID &&
        selectedSlot.id !== "";

      await creaPrenotazioneApi({
        slotId: hasRealSlot ? selectedSlot.id : null,
        medicoId: selectedMedico.id,
        dataOraInizio: selectedSlot.dataOraInizio,
        notePaziente: notePaziente.trim() || null,
      });
      toast.success("Prenotazione creata con successo.");
      router.replace("/dashboard/prenotazioni");
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Creazione prenotazione non riuscita."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Nuova prenotazione</h1>
        <p className="dash-page-subtitle">
          Scegli medico, giorno e fascia oraria tra gli slot generati dal medico.
        </p>
      </header>

      <section className="dash-panel space-y-4">
        <h2 className="dash-section-title">1. Ricerca medici</h2>
        <div className="dash-form-grid">
          <FancySelect
            label="Specializzazione"
            value={specializzazione}
            options={specializzazioneOptions}
            onChange={onSpecializzazioneChange}
            placeholder="Tutte le specializzazioni"
          />
          <FancySelect
            label="Medico"
            value={nessunMedicoDisponibile ? "" : selectedMedicoId}
            options={medicoOptions}
            onChange={onMedicoChange}
            disabled={loadingMedici || nessunMedicoDisponibile}
            invalid={nessunMedicoDisponibile}
            placeholder="Seleziona un medico"
          />
        </div>

        {selectedMedico && (
          <div className="dash-select-card is-selected">
            <strong>
              {selectedMedico.nome} {selectedMedico.cognome}
            </strong>
            <span>
              {selectedMedico.dettagliMedico?.specializzazione ?? "Specialista"}
              {selectedMedico.dettagliMedico
                ? ` · € ${selectedMedico.dettagliMedico.tariffaVisita}`
                : ""}
            </span>
          </div>
        )}
      </section>

      {selectedMedico && (
        <form className="dash-panel space-y-4" onSubmit={onSubmit}>
          <h2 className="dash-section-title">2. Giorno e orario</h2>

          {loadingSlots ? (
            <p className="text-sm text-muted">Caricamento disponibilità…</p>
          ) : (
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
                    const available = availableDays.has(cell.key);
                    const selected = selectedDay === cell.key;
                    const isToday = cell.key === todayKey;
                    const disabled = past || !available;

                    return (
                      <button
                        key={cell.key}
                        type="button"
                        className={[
                          "booking-cal-cell",
                          past ? "is-past" : "",
                          available ? "is-available" : "is-blocked",
                          selected ? "is-selected" : "",
                          isToday ? "is-today" : "",
                        ]
                          .filter(Boolean)
                          .join(" ")}
                        disabled={disabled}
                        onClick={() => onSelectDay(cell.key)}
                      >
                        {cell.date.getDate()}
                      </button>
                    );
                  })}
                </div>

                <p className="booking-cal-legend">
                  Solo i giorni con almeno una fascia libera (medico e paziente)
                  sono selezionabili. I giorni passati sono disabilitati.
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

                {!selectedDay ? (
                  <p className="text-sm text-muted">
                    Scegli una data nel calendario per vedere gli orari.
                  </p>
                ) : daySlots.length === 0 ? (
                  <p className="text-sm text-muted">
                    Nessun orario disponibile in questo giorno.
                  </p>
                ) : (
                  <>
                    {daySlots.some((s) => new Date(s.dataOraInizio).getHours() < 13) && (
                    <div className="booking-time-group">
                      <p className="booking-time-group-label">Mattina</p>
                      <div className="dash-slot-grid">
                        {daySlots
                          .filter((s) => {
                            const h = new Date(s.dataOraInizio).getHours();
                            return h < 13;
                          })
                          .map((slot) => (
                            <button
                              key={slot.dataOraInizio}
                              type="button"
                              className={`dash-slot${
                                selectedSlotKey === slot.dataOraInizio
                                  ? " is-selected"
                                  : ""
                              }`}
                              onClick={() =>
                                setSelectedSlotKey(slot.dataOraInizio)
                              }
                            >
                              {formatTime(slot.dataOraInizio)}
                            </button>
                          ))}
                      </div>
                    </div>
                    )}
                    {daySlots.some((s) => new Date(s.dataOraInizio).getHours() >= 13) && (
                    <div className="booking-time-group">
                      <p className="booking-time-group-label">
                        Pomeriggio
                      </p>
                      <div className="dash-slot-grid">
                        {daySlots
                          .filter((s) => {
                            const h = new Date(s.dataOraInizio).getHours();
                            return h >= 13;
                          })
                          .map((slot) => (
                            <button
                              key={slot.dataOraInizio}
                              type="button"
                              className={`dash-slot${
                                selectedSlotKey === slot.dataOraInizio
                                  ? " is-selected"
                                  : ""
                              }`}
                              onClick={() =>
                                setSelectedSlotKey(slot.dataOraInizio)
                              }
                            >
                              {formatTime(slot.dataOraInizio)}
                            </button>
                          ))}
                      </div>
                    </div>
                    )}
                  </>
                )}
              </div>
            </div>
          )}

          <h2 className="dash-section-title">3. Note sintomatiche</h2>
          <label className="dash-label">
            Descrizione sintomi / note
            <textarea
              className="auth-field dash-textarea"
              rows={4}
              value={notePaziente}
              onChange={(e) => setNotePaziente(e.target.value)}
              placeholder="Descrivi brevemente i sintomi o le motivazioni della visita"
            />
          </label>

          <button
            className="dash-btn"
            type="submit"
            disabled={submitting || !selectedSlot}
          >
            {submitting ? "Prenotazione…" : "Conferma prenotazione"}
          </button>
        </form>
      )}
    </div>
  );
}
