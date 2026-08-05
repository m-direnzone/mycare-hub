"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import { ApiError } from "@/src/lib/api";
import {
  fetchAgendaMedicoApi,
  fetchFascicoloApi,
  fetchMieVisitePazienteApi,
  formatDate,
  formatDateTime,
  statoLabel,
} from "@/src/lib/dashboard-api";
import type { PrenotazioneDto, RefertoDto } from "@/src/types/dto";
import { toast } from "react-toastify";

function VisitaRiepilogoCard({
  visita,
  showMedico = false,
}: {
  visita: PrenotazioneDto;
  showMedico?: boolean;
}) {
  return (
    <li className="dash-list-item dash-riepilogo-card">
      <div className="dash-list-main">
        {showMedico ? (
          <>
            <p className="dash-list-title">{visita.medicoNomeCompleto}</p>
            {visita.medicoSpecializzazione ? (
              <p className="dash-list-meta">{visita.medicoSpecializzazione}</p>
            ) : null}
          </>
        ) : (
          <p className="dash-list-title">{visita.pazienteNomeCompleto}</p>
        )}
        <p className="dash-list-meta">{formatDateTime(visita.dataOraInizio)}</p>
      </div>
      <span className={`dash-badge dash-badge-${visita.stato}`}>
        {statoLabel(visita.stato)}
      </span>
    </li>
  );
}

function MedicoVisiteRiepilogo() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      try {
        const data = await fetchAgendaMedicoApi();
        if (!cancelled) setVisite(data);
      } catch (err) {
        if (!cancelled) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare il riepilogo visite."
          );
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const inAttesa = useMemo(
    () =>
      [...visite]
        .filter((v) => v.stato === "in_attesa")
        .sort(
          (a, b) =>
            new Date(b.creatoAt).getTime() - new Date(a.creatoAt).getTime()
        )
        .slice(0, 5),
    [visite]
  );

  const prossime = useMemo(() => {
    const now = Date.now();
    return [...visite]
      .filter(
        (v) =>
          v.stato === "confermata" &&
          new Date(v.dataOraInizio).getTime() >= now
      )
      .sort(
        (a, b) =>
          new Date(a.dataOraInizio).getTime() -
          new Date(b.dataOraInizio).getTime()
      )
      .slice(0, 5);
  }, [visite]);

  return (
    <div className="dash-home-riepilogo">
      <section className="dash-panel">
        <div className="dash-page-header-row">
          <div>
            <h2 className="dash-section-title">In attesa di approvazione</h2>
            <p className="dash-page-subtitle" style={{ marginTop: "0.25rem" }}>
              Ultime 5 richieste da accettare o rifiutare.
            </p>
          </div>
          <Link href="/dashboard/visite?tab=in_attesa" className="dash-btn">
            Gestisci visite
          </Link>
        </div>
        {loading ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : inAttesa.length === 0 ? (
          <div className="dash-empty">
            <p>Nessuna visita in attesa di approvazione.</p>
          </div>
        ) : (
          <ul className="dash-list dash-riepilogo-list">
            {inAttesa.map((v) => (
              <VisitaRiepilogoCard key={v.id} visita={v} />
            ))}
          </ul>
        )}
      </section>

      <section className="dash-panel">
        <div className="dash-page-header-row">
          <div>
            <h2 className="dash-section-title">Prossime visite</h2>
            <p className="dash-page-subtitle" style={{ marginTop: "0.25rem" }}>
              Le 5 visite imminenti in agenda.
            </p>
          </div>
          <Link href="/dashboard/visite" className="dash-btn">
            Vai alle visite
          </Link>
        </div>
        {loading ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : prossime.length === 0 ? (
          <div className="dash-empty">
            <p>Nessuna visita in programma.</p>
          </div>
        ) : (
          <ul className="dash-list dash-riepilogo-list">
            {prossime.map((v) => (
              <VisitaRiepilogoCard key={v.id} visita={v} />
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function PazienteHomeRiepilogo() {
  const [visite, setVisite] = useState<PrenotazioneDto[]>([]);
  const [referti, setReferti] = useState<RefertoDto[]>([]);
  const [loadingVisite, setLoadingVisite] = useState(true);
  const [loadingReferti, setLoadingReferti] = useState(true);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingVisite(true);
      try {
        const data = await fetchMieVisitePazienteApi();
        if (!cancelled) setVisite(data);
      } catch (err) {
        if (!cancelled) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare il riepilogo prenotazioni."
          );
        }
      } finally {
        if (!cancelled) setLoadingVisite(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoadingReferti(true);
      try {
        const data = await fetchFascicoloApi();
        if (!cancelled) setReferti(data);
      } catch (err) {
        if (!cancelled) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare gli ultimi referti."
          );
        }
      } finally {
        if (!cancelled) setLoadingReferti(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const daConfermare = useMemo(
    () =>
      [...visite]
        .filter((v) => v.stato === "in_attesa")
        .sort(
          (a, b) =>
            new Date(b.creatoAt).getTime() - new Date(a.creatoAt).getTime()
        )
        .slice(0, 5),
    [visite]
  );

  const prossime = useMemo(() => {
    const now = Date.now();
    return [...visite]
      .filter(
        (v) =>
          v.stato === "confermata" &&
          new Date(v.dataOraInizio).getTime() >= now
      )
      .sort(
        (a, b) =>
          new Date(a.dataOraInizio).getTime() -
          new Date(b.dataOraInizio).getTime()
      )
      .slice(0, 5);
  }, [visite]);

  const ultimiReferti = useMemo(
    () =>
      [...referti]
        .sort(
          (a, b) =>
            new Date(b.dataEmissione).getTime() -
            new Date(a.dataEmissione).getTime()
        )
        .slice(0, 3),
    [referti]
  );

  return (
    <div className="dash-home-riepilogo">
      <section className="dash-panel">
        <div className="dash-page-header-row">
          <div>
            <h2 className="dash-section-title">Prossime visite</h2>
            <p className="dash-page-subtitle" style={{ marginTop: "0.25rem" }}>
              Le 5 visite programmate imminenti.
            </p>
          </div>
          <Link href="/dashboard/prenotazioni" className="dash-btn">
            Vai alle prenotazioni
          </Link>
        </div>
        {loadingVisite ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : prossime.length === 0 ? (
          <div className="dash-empty">
            <p>Nessuna visita in programma.</p>
          </div>
        ) : (
          <ul className="dash-list dash-riepilogo-list">
            {prossime.map((v) => (
              <VisitaRiepilogoCard key={v.id} visita={v} showMedico />
            ))}
          </ul>
        )}
      </section>

      <section className="dash-panel">
        <div className="dash-page-header-row">
          <div>
            <h2 className="dash-section-title">Da confermare</h2>
            <p className="dash-page-subtitle" style={{ marginTop: "0.25rem" }}>
              Ultime 5 richieste ancora in attesa.
            </p>
          </div>
          <Link href="/dashboard/prenotazioni" className="dash-btn">
            Gestisci prenotazioni
          </Link>
        </div>
        {loadingVisite ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : daConfermare.length === 0 ? (
          <div className="dash-empty">
            <p>Nessuna visita da confermare.</p>
          </div>
        ) : (
          <ul className="dash-list dash-riepilogo-list">
            {daConfermare.map((v) => (
              <VisitaRiepilogoCard key={v.id} visita={v} showMedico />
            ))}
          </ul>
        )}
      </section>

      <section className="dash-panel">
        <div className="dash-page-header-row">
          <div>
            <h2 className="dash-section-title">Ultimi referti</h2>
            <p className="dash-page-subtitle" style={{ marginTop: "0.25rem" }}>
              Gli ultimi 3 documenti del fascicolo sanitario.
            </p>
          </div>
          <Link href="/dashboard/fascicolo" className="dash-btn">
            Vai al fascicolo
          </Link>
        </div>
        {loadingReferti ? (
          <p className="text-sm text-muted">Caricamento…</p>
        ) : ultimiReferti.length === 0 ? (
          <div className="dash-empty">
            <p>Nessun referto disponibile.</p>
          </div>
        ) : (
          <ul className="dash-list dash-riepilogo-list dash-riepilogo-list-referti">
            {ultimiReferti.map((r) => (
              <li key={r.id} className="dash-list-item dash-riepilogo-card dash-riepilogo-card-referto">
                <div className="dash-list-main">
                  <p className="dash-list-title">{r.titolo}</p>
                  <p className="dash-list-meta">
                    {formatDate(r.dataEmissione)}
                  </p>
                  <p className="dash-list-meta">{r.medicoNomeCompleto}</p>
                </div>
                <a
                  href={r.downloadUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="dash-btn dash-btn-nowrap"
                >
                  Scarica PDF
                </a>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

export default function DashboardHomePage() {
  const { profilo } = useDashboard();
  const isPaziente = profilo.ruolo === "paziente";

  return (
    <div className="dash-page">
      <header className="dash-page-header">
        <h1 className="dash-page-title">
          Ciao, {profilo.nome || "utente"}
        </h1>
        <p className="dash-page-subtitle">
          {isPaziente
            ? "Gestisci prenotazioni, fascicolo sanitario e profilo personale."
            : "Gestisci agenda, visite e emissione referti dalla tua area dedicata."}
        </p>
      </header>

      <div className="dash-grid">
        {isPaziente ? (
          <>
            <Link href="/dashboard/prenotazioni/nuova" className="dash-tile">
              <h2>Nuova prenotazione</h2>
              <p>Cerca un medico e prenota uno slot disponibile.</p>
            </Link>
            <Link href="/dashboard/prenotazioni" className="dash-tile">
              <h2>Le mie prenotazioni</h2>
              <p>Consulta, conferma lo stato o cancella le visite.</p>
            </Link>
            <Link href="/dashboard/fascicolo" className="dash-tile">
              <h2>Fascicolo sanitario</h2>
              <p>Scarica i referti clinici in modo sicuro.</p>
            </Link>
            <Link href="/dashboard/profilo" className="dash-tile">
              <h2>Profilo personale</h2>
              <p>Verifica anagrafica e codice fiscale.</p>
            </Link>
          </>
        ) : (
          <>
            <Link href="/dashboard/agenda" className="dash-tile">
              <h2>Agenda & slot</h2>
              <p>Genera slot in blocco e blocca quelli liberi.</p>
            </Link>
            <Link href="/dashboard/visite" className="dash-tile">
              <h2>Gestione visite</h2>
              <p>Accetta richieste e concludi le visite.</p>
            </Link>
            <Link href="/dashboard/referti" className="dash-tile">
              <h2>Emissione referti</h2>
              <p>Carica PDF collegati a visita e paziente.</p>
            </Link>
            <Link href="/dashboard/profilo" className="dash-tile">
              <h2>Profilo personale</h2>
              <p>Dati anagrafici e professionali.</p>
            </Link>
          </>
        )}
      </div>

      {isPaziente ? <PazienteHomeRiepilogo /> : <MedicoVisiteRiepilogo />}
    </div>
  );
}
