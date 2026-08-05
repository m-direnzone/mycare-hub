"use client";

import { Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { ApiError } from "@/src/lib/api";
import {
  cambiaStatoPrenotazioneApi,
  concludiVisitaApi,
  fetchAgendaMedicoApi,
  formatDateTime,
  statoLabel,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto } from "@/src/types/dto";
import { toast } from "react-toastify";

type TabKey = "oggi" | "in_attesa" | "in_arrivo" | "storico";

const TAB_KEYS: TabKey[] = ["oggi", "in_attesa", "in_arrivo", "storico"];

function parseTab(value: string | null): TabKey {
  if (value && TAB_KEYS.includes(value as TabKey)) return value as TabKey;
  return "oggi";
}

function startOfDay(d = new Date()) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function endOfDay(d = new Date()) {
  const x = new Date(d);
  x.setHours(23, 59, 59, 999);
  return x;
}

function VisiteMedicoContent() {
  const searchParams = useSearchParams();
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [tab, setTab] = useState<TabKey>(() =>
    parseTab(searchParams.get("tab"))
  );
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [noteMediche, setNoteMediche] = useState<Record<string, string>>({});
  const [concludiOpen, setConcludiOpen] = useState<string | null>(null);

  useEffect(() => {
    setTab(parseTab(searchParams.get("tab")));
  }, [searchParams]);

  async function load() {
    setLoading(true);
    try {
      const data = await fetchAgendaMedicoApi();
      setVisite(
        [...data].sort(
          (a, b) =>
            new Date(a.dataOraInizio).getTime() -
            new Date(b.dataOraInizio).getTime()
        )
      );
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Impossibile caricare le visite."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const filtered = useMemo(() => {
    const todayStart = startOfDay().getTime();
    const todayEnd = endOfDay().getTime();

    return visite.filter((v) => {
      const t = new Date(v.dataOraInizio).getTime();
      if (tab === "oggi") {
        return t >= todayStart && t <= todayEnd && v.stato === "confermata";
      }
      if (tab === "in_attesa") {
        return v.stato === "in_attesa";
      }
      if (tab === "in_arrivo") {
        return t > todayEnd && v.stato === "confermata";
      }
      return (
        v.stato !== "in_attesa" &&
        (t < todayStart ||
          v.stato === "completata" ||
          v.stato === "cancellata")
      );
    });
  }, [visite, tab]);

  async function onAccetta(id: string) {
    setBusyId(id);
    try {
      await cambiaStatoPrenotazioneApi(id, "confermata");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Accettazione non riuscita."
      );
    } finally {
      setBusyId(null);
    }
  }

  async function onCancella(id: string) {
    const motivo = prompt("Motivo della cancellazione (opzionale):");
    if (motivo === null) return;
    setBusyId(id);
    try {
      await cambiaStatoPrenotazioneApi(id, "cancellata");
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Cancellazione non riuscita."
      );
    } finally {
      setBusyId(null);
    }
  }

  async function onConcludi(id: string) {
    const note = (noteMediche[id] ?? "").trim();
    if (!note) {
      toast.error("Inserisci il resoconto clinico per concludere la visita.");
      return;
    }
    setBusyId(id);
    try {
      await concludiVisitaApi(id, { noteMediche: note });
      setConcludiOpen(null);
      await load();
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.message : "Conclusione non riuscita."
      );
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">Gestione visite</h1>
        <p className="dash-page-subtitle">
          Registro visite: oggi, in attesa, in arrivo e storico.
        </p>
      </header>

      <div className="dash-tabs">
        {(
          [
            ["oggi", "Oggi"],
            ["in_attesa", "In attesa"],
            ["in_arrivo", "In arrivo"],
            ["storico", "Storico"],
          ] as const
        ).map(([key, label]) => (
          <button
            key={key}
            type="button"
            className={`dash-tab ${tab === key ? "is-active" : ""}`}
            onClick={() => setTab(key)}
          >
            {label}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-sm text-muted">Caricamento…</p>
      ) : filtered.length === 0 ? (
        <div className="dash-empty">
          <p>Nessuna visita in questa sezione.</p>
        </div>
      ) : (
        <ul className="dash-list">
          {filtered.map((v) => (
            <li key={v.id} className="dash-list-item dash-list-item-col">
              <div className="dash-list-item-row">
                <div className="dash-list-main">
                  <p className="dash-list-title">{v.pazienteNomeCompleto}</p>
                  <p className="dash-list-meta">
                    CF {v.pazienteCodiceFiscale} ·{" "}
                    {formatDateTime(v.dataOraInizio)}
                  </p>
                  {v.notePaziente && (
                    <p className="dash-list-note">
                      Note paziente: {v.notePaziente}
                    </p>
                  )}
                  {v.noteMediche && (
                    <p className="dash-list-note">
                      Note mediche: {v.noteMediche}
                    </p>
                  )}
                </div>
                <div className="dash-list-actions">
                  <span className={`dash-badge dash-badge-${v.stato}`}>
                    {statoLabel(v.stato)}
                  </span>
                  {v.stato === "in_attesa" && (
                    <>
                      <button
                        type="button"
                        className="dash-btn"
                        disabled={busyId === v.id}
                        onClick={() => void onAccetta(v.id)}
                      >
                        Accetta
                      </button>
                      <button
                        type="button"
                        className="dash-btn dash-btn-danger"
                        disabled={busyId === v.id}
                        onClick={() => void onCancella(v.id)}
                      >
                        Cancella
                      </button>
                    </>
                  )}
                  {v.stato === "confermata" && (
                    <>
                      <button
                        type="button"
                        className="dash-btn"
                        onClick={() =>
                          setConcludiOpen((cur) =>
                            cur === v.id ? null : v.id
                          )
                        }
                      >
                        Concludi
                      </button>
                      <button
                        type="button"
                        className="dash-btn dash-btn-danger"
                        disabled={busyId === v.id}
                        onClick={() => void onCancella(v.id)}
                      >
                        Cancella
                      </button>
                    </>
                  )}
                </div>
              </div>

              {concludiOpen === v.id && (
                <div className="dash-inline-form">
                  <label className="dash-label">
                    Resoconto clinico
                    <textarea
                      className="auth-field dash-textarea"
                      rows={3}
                      value={noteMediche[v.id] ?? ""}
                      onChange={(e) =>
                        setNoteMediche((prev) => ({
                          ...prev,
                          [v.id]: e.target.value,
                        }))
                      }
                      placeholder="Inserisci le note mediche della visita"
                    />
                  </label>
                  <button
                    type="button"
                    className="dash-btn"
                    disabled={busyId === v.id}
                    onClick={() => void onConcludi(v.id)}
                  >
                    {busyId === v.id ? "Salvataggio…" : "Salva e concludi"}
                  </button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default function VisiteMedicoPage() {
  return (
    <Suspense fallback={<p className="text-sm text-muted">Caricamento…</p>}>
      <VisiteMedicoContent />
    </Suspense>
  );
}
