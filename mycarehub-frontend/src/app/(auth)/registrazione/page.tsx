"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import type { PazienteRegistrazioneDto } from "@/src/types/dto";
import {
  CODICE_FISCALE_GIA_REGISTRATO_MESSAGE,
  isCodiceFiscaleTakenError,
  isExistingAccountError,
  mapAuthErrorMessage,
  registerPaziente,
} from "@/src/lib/supabase/client";

const CF_REGEX = /^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$/;

const EXISTING_EMAIL_MESSAGE =
  "Esiste già un account con questa email. Se sei un medico creato dall'amministrazione, accedi con la password temporanea ricevuta via email: al primo accesso ti verrà chiesto di impostarne una nuova.";

export default function RegistrazionePage() {
  const router = useRouter();
  const [form, setForm] = useState<PazienteRegistrazioneDto>({
    email: "",
    password: "",
    nome: "",
    cognome: "",
    codiceFiscale: "",
    dataNascita: "",
    telefono: "",
  });
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  function updateField<K extends keyof PazienteRegistrazioneDto>(
    key: K,
    value: PazienteRegistrazioneDto[K]
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setInfo(null);

    const codiceFiscale = form.codiceFiscale.trim().toUpperCase();
    if (!CF_REGEX.test(codiceFiscale)) {
      setError("Formato codice fiscale non valido.");
      return;
    }

    setLoading(true);

    try {
      await registerPaziente({
        ...form,
        email: form.email.trim(),
        nome: form.nome.trim(),
        cognome: form.cognome.trim(),
        codiceFiscale,
        telefono: form.telefono?.trim() || null,
      });

      router.replace("/dashboard");
    } catch (err) {
      if (isCodiceFiscaleTakenError(err)) {
        setInfo(CODICE_FISCALE_GIA_REGISTRATO_MESSAGE);
      } else if (isExistingAccountError(err)) {
        setInfo(EXISTING_EMAIL_MESSAGE);
      } else {
        setError(
          mapAuthErrorMessage(err, "Registrazione non riuscita. Riprova.")
        );
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-foreground">
          Registrazione paziente
        </h1>
        <p className="mt-1 text-sm text-muted">
          Il ruolo è assegnato automaticamente come paziente. I medici vengono
          creati solo dall&apos;amministrazione.
        </p>
      </div>

      {error ? <div className="auth-alert" role="alert">{error}</div> : null}
      {info ? (
        <div className="auth-alert auth-alert-info" role="status">
          {info}{" "}
          <Link href="/login" className="auth-link">
            Vai al login
          </Link>
        </div>
      ) : null}

      <form className="space-y-4" onSubmit={onSubmit}>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block space-y-1.5 text-sm">
            <span className="font-medium">Nome</span>
            <input
              className="auth-field"
              name="nome"
              autoComplete="given-name"
              required
              maxLength={100}
              value={form.nome}
              onChange={(e) => updateField("nome", e.target.value)}
            />
          </label>

          <label className="block space-y-1.5 text-sm">
            <span className="font-medium">Cognome</span>
            <input
              className="auth-field"
              name="cognome"
              autoComplete="family-name"
              required
              maxLength={100}
              value={form.cognome}
              onChange={(e) => updateField("cognome", e.target.value)}
            />
          </label>
        </div>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Codice fiscale</span>
          <input
            className="auth-field uppercase tracking-wide"
            name="codiceFiscale"
            required
            maxLength={16}
            value={form.codiceFiscale}
            onChange={(e) => updateField("codiceFiscale", e.target.value.toUpperCase())}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Data di nascita</span>
          <input
            className="auth-field"
            type="date"
            name="dataNascita"
            required
            value={form.dataNascita}
            onChange={(e) => updateField("dataNascita", e.target.value)}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Telefono (opzionale)</span>
          <input
            className="auth-field"
            type="tel"
            name="telefono"
            autoComplete="tel"
            maxLength={20}
            value={form.telefono ?? ""}
            onChange={(e) => updateField("telefono", e.target.value)}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Email</span>
          <input
            className="auth-field"
            type="email"
            name="email"
            autoComplete="email"
            required
            value={form.email}
            onChange={(e) => updateField("email", e.target.value)}
          />
        </label>

        <label className="block space-y-1.5 text-sm">
          <span className="font-medium">Password</span>
          <input
            className="auth-field"
            type="password"
            name="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={form.password}
            onChange={(e) => updateField("password", e.target.value)}
          />
        </label>

        <input type="hidden" name="ruolo" value="paziente" readOnly />

        <button className="auth-button" type="submit" disabled={loading}>
          {loading ? "Creazione account…" : "Crea account"}
        </button>
      </form>

      <p className="text-center text-sm text-muted">
        Hai già un account?{" "}
        <Link href="/login" className="auth-link">
          Accedi
        </Link>
      </p>
    </div>
  );
}
