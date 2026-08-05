"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { DashboardProvider } from "@/src/components/dashboard/DashboardContext";
import { Header } from "@/src/components/dashboard/Header";
import { Sidebar } from "@/src/components/dashboard/Sidebar";
import { requireAdminFromProfilo } from "@/src/lib/admin-guard";
import { canAccessPath } from "@/src/lib/nav";
import { fetchMioProfiloFromDb } from "@/src/lib/profilo";
import type { ProfiloDto } from "@/src/types/dto";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const [profilo, setProfilo] = useState<ProfiloDto | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const loadProfilo = useCallback(async () => {
    const mio = await fetchMioProfiloFromDb();
    if (!mio) throw new Error("Profilo non trovato.");
    return mio;
  }, []);

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
      setEmail(result.user.email ?? result.profilo.email ?? null);
      setLoading(false);
    })();

    return () => {
      active = false;
    };
  }, [router]);

  useEffect(() => {
    if (!profilo) return;
    if (!canAccessPath(profilo.ruolo, pathname)) {
      router.replace("/admin");
    }
  }, [profilo, pathname, router]);

  useEffect(() => {
    setSidebarOpen(false);
  }, [pathname]);

  if (loading) {
    return (
      <main className="dash-loading">
        <p className="text-sm text-muted">Caricamento area admin…</p>
      </main>
    );
  }

  if (error || !profilo) {
    return (
      <main className="dash-loading">
        <p className="auth-alert">{error ?? "Profilo non disponibile."}</p>
      </main>
    );
  }

  return (
    <DashboardProvider
      profilo={profilo}
      email={email}
      onRefresh={loadProfilo}
    >
      <div className="dash-shell">
        <Sidebar
          ruolo="admin"
          open={sidebarOpen}
          onClose={() => setSidebarOpen(false)}
        />
        <div className="dash-main">
          <Header onMenuToggle={() => setSidebarOpen((v) => !v)} />
          <div className="dash-content">{children}</div>
        </div>
      </div>
      <ToastContainer
        position="bottom-right"
        autoClose={4000}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        pauseOnHover
        theme="colored"
        limit={3}
        style={{ width: "min(22rem, calc(100vw - 1.5rem))" }}
      />
    </DashboardProvider>
  );
}
