"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { createMedico } from "@/src/lib/medici";
import { fetchSpecializzazioniNomi } from "@/src/lib/specializzazioni";
import type { MedicoCreateResponseDto, MedicoOnboardingDto } from "@/src/types/dto";
import { toast } from "react-toastify";

const CF_REGEX = /^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$/;

const EMPTY_FORM: MedicoOnboardingDto = {
  email: "",
  nome: "",
  cognome: "",
  codiceFiscale: "",
  dataNascita: "",
  telefono: "",
  specializzazione: "",
  numeroIscrizioneAlbo: "",
  biografia: "",
  tariffaVisita: 80,
};

export default function NuovoMedicoAdminPage() {
  const [form, setForm] = useState<MedicoOnboardingDto>(EMPTY_FORM);
  const [success, setSuccess] = useState<MedicoCreateResponseDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  const [specializzazioni, setSpecializzazioni] = useState<string[]>([]);

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

  function updateField<K extends keyof MedicoOnboardingDto>(
    key: K,
    value: MedicoOnboardingDto[K]
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSuccess(null);
    setCopied(false);

    const codiceFiscale = form.codiceFiscale.trim().toUpperCase();
    if (!CF_REGEX.test(codiceFiscale)) {
      toast.error("Formato codice fiscale non valido.");
      return;
    }

    if (form.tariffaVisita < 0.01 || form.tariffaVisita > 10000) {
      toast.error("La tariffa deve essere tra 0,01 e 10.000 €.");
      return;
    }

    setLoading(true);

    try {
      const created = await createMedico({
        ...form,
        email: form.email.trim().toLowerCase(),
        nome: form.nome.trim(),
        cognome: form.cognome.trim(),
        codiceFiscale,
        telefono: form.telefono?.trim() || null,
        specializzazione: form.specializzazione.trim(),
        numeroIscrizioneAlbo: form.numeroIscrizioneAlbo.trim(),
        biografia: form.biografia?.trim() || null,
        tariffaVisita: Number(form.tariffaVisita),
      });

      setSuccess(created);
      setForm(EMPTY_FORM);
    } catch (err) {
      toast.error(
        err instanceof Error
          ? err.message
          : "Creazione medico non riuscita. Riprova."
      );
    } finally {
      setLoading(false);
    }
  }

  async function copyPassword() {
    if (!success?.passwordTemporanea) return;
    try {
      await navigator.clipboard.writeText(success.passwordTemporanea);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  return (
    <div className="dash-page">
      <header className="dash-page-header dash-page-header-row">
        <div>
          <h1 className="dash-page-title">Nuovo medico</h1>
          <p className="dash-page-subtitle">
            Crea account Auth, profilo e dettagli professionali. Il medico
            dovrà cambiare la password temporanea al primo accesso.
          </p>
        </div>
        <Link href="/admin/medici" className="dash-btn dash-btn-ghost">
          Torna all&apos;elenco
        </Link>
      </header>

      {success ? (
        <div className="auth-alert auth-alert-info space-y-3" role="status">
          <p>{success.messaggio}</p>
          <p className="text-sm">
            <span className="font-medium">Email:</span> {success.email}
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <code className="rounded-lg bg-white/70 px-3 py-2 text-sm tracking-wide">
              {success.passwordTemporanea}
            </code>
            <button
              type="button"
              className="dash-btn dash-btn-ghost"
              onClick={copyPassword}
            >
              {copied ? "Copiata" : "Copia password"}
            </button>
          </div>
        </div>
      ) : null}

      <form className="space-y-5" onSubmit={onSubmit}>
        <section className="dash-panel space-y-4">
          <div className="border-b border-[var(--border)] pb-3">
            <h2 className="dash-section-title">Dati anagrafici</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Informazioni personali del medico
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="dash-label">
              Nome
              <input
                className="auth-field"
                required
                maxLength={100}
                value={form.nome}
                onChange={(e) => updateField("nome", e.target.value)}
              />
            </label>
            <label className="dash-label">
              Cognome
              <input
                className="auth-field"
                required
                maxLength={100}
                value={form.cognome}
                onChange={(e) => updateField("cognome", e.target.value)}
              />
            </label>
            <label className="dash-label">
              Codice fiscale
              <input
                className="auth-field uppercase tracking-wide"
                required
                maxLength={16}
                value={form.codiceFiscale}
                onChange={(e) =>
                  updateField("codiceFiscale", e.target.value.toUpperCase())
                }
              />
            </label>
            <label className="dash-label">
              Data di nascita
              <input
                className="auth-field"
                type="date"
                required
                value={form.dataNascita}
                onChange={(e) => updateField("dataNascita", e.target.value)}
              />
            </label>
          </div>
        </section>

        <section className="dash-panel space-y-4">
          <div className="border-b border-[var(--border)] pb-3">
            <h2 className="dash-section-title">Contatti</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Credenziali di accesso e recapito
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="dash-label sm:col-span-2 lg:col-span-1">
              Email
              <input
                className="auth-field"
                type="email"
                required
                value={form.email}
                onChange={(e) => updateField("email", e.target.value)}
              />
            </label>
            <label className="dash-label sm:col-span-2 lg:col-span-1">
              Telefono (opzionale)
              <input
                className="auth-field"
                type="tel"
                maxLength={20}
                value={form.telefono ?? ""}
                onChange={(e) => updateField("telefono", e.target.value)}
              />
            </label>
          </div>
        </section>

        <section className="dash-panel space-y-4">
          <div className="border-b border-[var(--border)] pb-3">
            <h2 className="dash-section-title">Profilo professionale</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">
              Specializzazione, albo e tariffario
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <label className="dash-label sm:col-span-2 lg:col-span-1">
              Specializzazione
              <input
                className="auth-field"
                list="specializzazioni-list"
                required
                maxLength={150}
                value={form.specializzazione}
                onChange={(e) => updateField("specializzazione", e.target.value)}
              />
              <datalist id="specializzazioni-list">
                {specializzazioni.map((item) => (
                  <option key={item} value={item} />
                ))}
              </datalist>
            </label>
            <label className="dash-label">
              N. iscrizione albo
              <input
                className="auth-field"
                required
                maxLength={50}
                value={form.numeroIscrizioneAlbo}
                onChange={(e) =>
                  updateField("numeroIscrizioneAlbo", e.target.value)
                }
              />
            </label>
            <label className="dash-label">
              Tariffa visita (€)
              <input
                className="auth-field"
                type="number"
                required
                min={0.01}
                max={10000}
                step={0.01}
                value={form.tariffaVisita}
                onChange={(e) =>
                  updateField("tariffaVisita", Number(e.target.value))
                }
              />
            </label>
            <label className="dash-label sm:col-span-2 lg:col-span-3">
              Biografia (opzionale)
              <textarea
                className="auth-field dash-textarea"
                maxLength={2000}
                value={form.biografia ?? ""}
                onChange={(e) => updateField("biografia", e.target.value)}
              />
            </label>
          </div>
        </section>

        <div className="flex justify-end pt-1">
          <button className="dash-btn min-w-[10rem]" type="submit" disabled={loading}>
            {loading ? "Creazione in corso…" : "Crea medico"}
          </button>
        </div>
      </form>
    </div>
  );
}
