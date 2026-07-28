"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import {
  login,
  needsPasswordChange,
} from "@/src/lib/supabase/client";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const { user } = await login(email.trim(), password);

      if (needsPasswordChange(user)) {
        router.replace("/cambia-password");
        return;
      }

      router.replace("/dashboard");
    } catch (err) {
      const message =
        err instanceof Error
          ? err.message
          : "Accesso non riuscito. Controlla le credenziali.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-foreground">
          Accedi
        </h1>
        <p className="mt-1 text-sm text-muted">
          Usa email e password per entrare nella dashboard.
        </p>
      </div>

      {error ? <div className="auth-alert" role="alert">{error}</div> : null}

      <form className="space-y-4" onSubmit={onSubmit}>
        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Email</span>
          <input
            className="auth-field"
            type="email"
            name="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Password</span>
          <input
            className="auth-field"
            type="password"
            name="password"
            autoComplete="current-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>

        <p className="text-xs leading-relaxed text-muted">
          Sei un medico? Al primo accesso usa la password temporanea ricevuta
          via email, poi ti chiederemo di impostarne una nuova.
        </p>

        <button className="auth-button" type="submit" disabled={loading}>
          {loading ? "Accesso in corso…" : "Entra"}
        </button>
      </form>

      <p className="text-center text-sm text-muted">
        Non hai un account?{" "}
        <Link href="/registrazione" className="auth-link">
          Registrati come paziente
        </Link>
      </p>
    </div>
  );
}
