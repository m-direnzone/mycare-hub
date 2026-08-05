"use client";

import { useEffect, useState } from "react";
import {
  fetchAdminKpi,
  formatDelta,
  formatEuro,
  type AdminKpiDto,
} from "@/src/lib/admin-api";
import { ApiError } from "@/src/lib/api";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import { toast } from "react-toastify";

export default function AdminDashboardPage() {
  const { profilo } = useDashboard();
  const [kpi, setKpi] = useState<AdminKpiDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const data = await fetchAdminKpi();
        if (active) setKpi(data);
      } catch (err) {
        if (active) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare i KPI."
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
        <h1 className="dash-page-title">Dashboard admin</h1>
        <p className="dash-page-subtitle">
          Ciao {profilo.nome}, ecco lo stato operativo della piattaforma.
        </p>
      </header>

      {loading ? (
        <p className="text-sm text-muted">Caricamento indicatori…</p>
      ) : null}

      {kpi ? (
        <>
          <section className="admin-kpi-section">
            <h2 className="dash-section-title">Volume attivo</h2>
            <div className="admin-kpi-grid">
              <article className="admin-kpi-card">
                <p className="admin-kpi-label">Pazienti registrati</p>
                <p className="admin-kpi-value">{kpi.totalePazientiRegistrati}</p>
                <p className="admin-kpi-delta">
                  Crescita mensile{" "}
                  <span
                    className={
                      kpi.deltaCrescitaMensilePercentuale >= 0
                        ? "is-up"
                        : "is-down"
                    }
                  >
                    {formatDelta(kpi.deltaCrescitaMensilePercentuale)}
                  </span>
                  <span className="admin-kpi-hint">
                    {" "}
                    ({kpi.pazientiNuoviMeseCorrente} questo mese ·{" "}
                    {kpi.pazientiNuoviMesePrecedente} mese scorso)
                  </span>
                </p>
              </article>
              <article className="admin-kpi-card">
                <p className="admin-kpi-label">Medici attivi</p>
                <p className="admin-kpi-value">{kpi.totaleMediciAttivi}</p>
                <p className="admin-kpi-delta">
                  Password cambiata dopo il primo accesso
                </p>
              </article>
              <article className="admin-kpi-card">
                <p className="admin-kpi-label">In attesa di attivazione</p>
                <p className="admin-kpi-value">
                  {kpi.mediciInAttesaAttivazione}
                </p>
                <p className="admin-kpi-delta">
                  Medici ancora con password temporanea
                </p>
              </article>
            </div>
          </section>

          <section className="admin-kpi-section">
            <h2 className="dash-section-title">Flusso clinico</h2>
            <div className="admin-kpi-grid">
              <article className="admin-kpi-card">
                <p className="admin-kpi-label">Visite prenotate (mese)</p>
                <p className="admin-kpi-value">
                  {kpi.visitePrenotateMeseCorrente}
                </p>
              </article>
              <article className="admin-kpi-card">
                <p className="admin-kpi-label">Tasso di completamento</p>
                <p className="admin-kpi-value">
                  {kpi.tassoCompletamentoPercentuale.toFixed(1)}%
                </p>
                <p className="admin-kpi-delta">
                  {kpi.visiteCompletateMeseCorrente} completate ·{" "}
                  {kpi.visiteCancellateOSaltateMeseCorrente} cancellate/saltate
                </p>
              </article>
            </div>
          </section>

          <section className="admin-kpi-section">
            <h2 className="dash-section-title">Economico / finanziario</h2>
            <div className="admin-kpi-grid">
              <article className="admin-kpi-card admin-kpi-card-wide">
                <p className="admin-kpi-label">
                  Volume d&apos;affari intermediato (mese)
                </p>
                <p className="admin-kpi-value">
                  {formatEuro(kpi.volumeAffariIntermediatoMese)}
                </p>
                <p className="admin-kpi-delta">
                  Somma tariffe delle visite completate nel mese corrente
                </p>
              </article>
            </div>
          </section>
        </>
      ) : null}
    </div>
  );
}
