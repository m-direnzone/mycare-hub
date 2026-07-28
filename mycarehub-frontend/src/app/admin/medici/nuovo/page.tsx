"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { requireAdminFromProfilo } from "@/src/lib/admin-guard";
import { createMedico } from "@/src/lib/medici";
import { logout } from "@/src/lib/supabase/client";
import type { MedicoCreateResponseDto, MedicoOnboardingDto } from "@/src/types/dto";

const CF_REGEX = /^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$/;

const SPECIALIZZAZIONI = [
  "Allergologia",
  "Anestesia e rianimazione",
  "Cardiologia",
  "Chirurgia generale",
  "Dermatologia",
  "Endocrinologia",
  "Gastroenterologia",
  "Geriatria",
  "Ginecologia",
  "Medicina interna",
  "Neurologia",
  "Oculistica",
  "Ortopedia",
  "Otorinolaringoiatria",
  "Pediatria",
  "Pneumologia",
  "Psichiatria",
  "Radiologia",
  "Reumatologia",
  "Urologia",
];

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
  const router = useRouter();
  const [checking, setChecking] = useState(true);
  const [form, setForm] = useState<MedicoOnboardingDto>(EMPTY_FORM);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<MedicoCreateResponseDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let active = true;

    (async () => {
      const result = await requireAdminFromProfilo();
      if (!active) return;

      if (!result.ok) {
        router.replace(result.redirect);
        return;
      }

      setChecking(false);
    })();

    return () => {
      active = false;
    };
  }, [router]);

  function updateField<K extends keyof MedicoOnboardingDto>(
    key: K,
    value: MedicoOnboardingDto[K]
  ) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setCopied(false);

    const codiceFiscale = form.codiceFiscale.trim().toUpperCase();
    if (!CF_REGEX.test(codiceFiscale)) {
      setError("Formato codice fiscale non valido.");
      return;
    }

    if (form.tariffaVisita < 0.01 || form.tariffaVisita > 10000) {
      setError("La tariffa deve essere tra 0,01 e 10.000 €.");
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
      setError(
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

  async function onLogout() {
    await logout();
    router.replace("/login");
  }

  if (checking) {
    return (
      <main className="auth-shell">
        <p className="text-sm text-muted">Verifica permessi admin…</p>
      </main>
    );
  }

  return (
    <main className="auth-shell">
      <div className="auth-panel auth-panel-wide space-y-6">
        <div className="text-center">
          <p className="auth-brand">MyCareHub</p>
          <h1 className="mt-3 text-xl font-semibold tracking-tight">
            Inserisci nuovo medico
          </h1>
          <p className="mt-1 text-sm text-muted">
            Crea l&apos;account Auth, il profilo e i dettagli professionali.
            Il medico riceverà una password temporanea da cambiare al primo accesso.
          </p>
        </div>

        {error ? <div className="auth-alert" role="alert">{error}</div> : null}

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
                className="auth-button w-auto px-4 py-2 text-sm"
                onClick={copyPassword}
              >
                {copied ? "Copiata" : "Copia password"}
              </button>
            </div>
          </div>
        ) : null}

        <form className="space-y-4" onSubmit={onSubmit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block space-y-1.5 text-sm">
              <span className="font-medium">Nome</span>
              <input
                className="auth-field"
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
              required
              maxLength={16}
              value={form.codiceFiscale}
              onChange={(e) =>
                updateField("codiceFiscale", e.target.value.toUpperCase())
              }
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block space-y-1.5 text-sm">
              <span className="font-medium">Data di nascita</span>
              <input
                className="auth-field"
                type="date"
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
                maxLength={20}
                value={form.telefono ?? ""}
                onChange={(e) => updateField("telefono", e.target.value)}
              />
            </label>
          </div>

          <label className="block space-y-1.5 text-sm">
            <span className="font-medium">Email</span>
            <input
              className="auth-field"
              type="email"
              required
              value={form.email}
              onChange={(e) => updateField("email", e.target.value)}
            />
          </label>

          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block space-y-1.5 text-sm">
              <span className="font-medium">Specializzazione</span>
              <input
                className="auth-field"
                list="specializzazioni-list"
                required
                maxLength={150}
                value={form.specializzazione}
                onChange={(e) => updateField("specializzazione", e.target.value)}
              />
              <datalist id="specializzazioni-list">
                {SPECIALIZZAZIONI.map((item) => (
                  <option key={item} value={item} />
                ))}
              </datalist>
            </label>
            <label className="block space-y-1.5 text-sm">
              <span className="font-medium">N. iscrizione albo</span>
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
          </div>

          <label className="block space-y-1.5 text-sm">
            <span className="font-medium">Tariffa visita (€)</span>
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

          <label className="block space-y-1.5 text-sm">
            <span className="font-medium">Biografia (opzionale)</span>
            <textarea
              className="auth-field min-h-28 resize-y"
              maxLength={2000}
              value={form.biografia ?? ""}
              onChange={(e) => updateField("biografia", e.target.value)}
            />
          </label>

          <button className="auth-button" type="submit" disabled={loading}>
            {loading ? "Creazione in corso…" : "Crea medico"}
          </button>
        </form>

        <div className="flex flex-col items-center gap-2 text-sm">
          <Link href="/admin" className="auth-link">
            Torna al pannello admin
          </Link>
          <button
            type="button"
            className="auth-link"
            onClick={onLogout}
          >
            Esci
          </button>
        </div>
      </div>
    </main>
  );
}
