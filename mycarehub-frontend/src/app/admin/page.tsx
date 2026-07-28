"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { requireAdminFromProfilo } from "@/src/lib/admin-guard";
import { logout } from "@/src/lib/supabase/client";
import type { ProfiloDto } from "@/src/types/dto";

export default function AdminDashboardPage() {
  const router = useRouter();
  const [profilo, setProfilo] = useState<ProfiloDto | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    (async () => {
      const result = await requireAdminFromProfilo();
      if (!active) return;

      if (!result.ok) {
        router.replace(result.redirect);
        return;
      }

      setProfilo(result.profilo);
      setEmail(result.user.email ?? null);
      setLoading(false);
    })();

    return () => {
      active = false;
    };
  }, [router]);

  async function onLogout() {
    await logout();
    router.replace("/login");
  }

  if (loading) {
    return (
      <main className="auth-shell">
        <p className="text-sm text-muted">Caricamento pannello admin…</p>
      </main>
    );
  }

  return (
    <main className="auth-shell">
      <div className="auth-panel auth-panel-wide space-y-6">
        <div className="text-center">
          <p className="auth-brand">MyCareHub</p>
          <h1 className="mt-3 text-xl font-semibold tracking-tight">
            Pannello amministrazione
          </h1>
          <p className="mt-1 text-sm text-muted">
            Accesso riservato agli account con ruolo{" "}
            <span className="font-medium text-foreground">admin</span> in
            profilo.
          </p>
        </div>

        <div className="rounded-xl border border-[var(--border)] bg-white/55 px-4 py-3 text-sm">
          <p>
            <span className="font-medium">Operatore:</span>{" "}
            {profilo ? `${profilo.nome} ${profilo.cognome}` : "—"}
          </p>
          <p className="mt-1 text-muted">
            {email ?? "email non disponibile"} · ruolo {profilo?.ruolo}
          </p>
        </div>

        <section className="space-y-3">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
            Azioni disponibili
          </h2>
          <Link
            href="/admin/medici/nuovo"
            className="auth-button inline-flex items-center justify-center no-underline"
          >
            Inserisci nuovo medico
          </Link>
        </section>

        <div className="flex flex-col items-center gap-2 pt-2 text-sm">
          <button className="auth-button" type="button" onClick={onLogout}>
            Esci
          </button>
          <Link href="/" className="auth-link">
            Torna alla home
          </Link>
        </div>
      </div>
    </main>
  );
}
