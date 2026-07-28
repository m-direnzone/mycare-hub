"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  getCurrentUser,
  logout,
  needsPasswordChange,
} from "@/src/lib/supabase/client";

export default function DashboardPage() {
  const router = useRouter();
  const [email, setEmail] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    (async () => {
      try {
        const user = await getCurrentUser();
        if (!active) return;

        if (!user) {
          router.replace("/login");
          return;
        }

        if (needsPasswordChange(user)) {
          router.replace("/cambia-password");
          return;
        }

        setEmail(user.email ?? null);
        setLoading(false);
      } catch {
        if (active) {
          router.replace("/login");
        }
      }
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
        <p className="text-sm text-muted">Caricamento dashboard…</p>
      </main>
    );
  }

  return (
    <main className="auth-shell">
      <div className="auth-panel space-y-4 text-center">
        <p className="auth-brand">MyCareHub</p>
        <h1 className="text-xl font-semibold">Dashboard</h1>
        <p className="text-sm text-muted">
          Sessione attiva{email ? ` per ${email}` : ""}.
        </p>
        <button className="auth-button" type="button" onClick={onLogout}>
          Esci
        </button>
        <Link href="/" className="auth-link block text-sm">
          Torna alla home
        </Link>
      </div>
    </main>
  );
}
