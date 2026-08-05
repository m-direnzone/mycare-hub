"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  changePasswordAndClearTempFlag,
  getCurrentUser,
  needsPasswordChange,
} from "@/src/lib/supabase/client";
import { resolveHomePathFromProfilo } from "@/src/lib/routing";
import { toast } from "react-toastify";

export default function CambiaPasswordPage() {
  const router = useRouter();
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [checking, setChecking] = useState(true);

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

        if (!needsPasswordChange(user)) {
          router.replace(await resolveHomePathFromProfilo());
          return;
        }
      } finally {
        if (active) setChecking(false);
      }
    })();

    return () => {
      active = false;
    };
  }, [router]);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (password.length < 8) {
      toast.error("La nuova password deve avere almeno 8 caratteri.");
      return;
    }

    if (password !== confirm) {
      toast.error("Le password non coincidono.");
      return;
    }

    setLoading(true);

    try {
      await changePasswordAndClearTempFlag(password);
      router.replace(await resolveHomePathFromProfilo());
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : "Impossibile aggiornare la password."
      );
    } finally {
      setLoading(false);
    }
  }

  if (checking) {
    return <p className="text-sm text-muted">Verifica sessione…</p>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-foreground">
          Imposta nuova password
        </h1>
        <p className="mt-1 text-sm text-muted">
          Al primo accesso è necessario sostituire la password temporanea
          ricevuta via email.
        </p>
      </div>

      <form className="space-y-4" onSubmit={onSubmit}>
        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Nuova password</span>
          <input
            className="auth-field"
            type="password"
            name="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Conferma password</span>
          <input
            className="auth-field"
            type="password"
            name="confirmPassword"
            autoComplete="new-password"
            required
            minLength={8}
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
          />
        </label>

        <button className="auth-button" type="submit" disabled={loading}>
          {loading ? "Salvataggio…" : "Salva e continua"}
        </button>
      </form>

      <p className="text-center text-sm text-muted">
        <Link href="/login" className="auth-link">
          Torna al login
        </Link>
      </p>
    </div>
  );
}
