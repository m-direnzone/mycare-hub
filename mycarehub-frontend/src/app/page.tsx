import Link from "next/link";

export default function HomePage() {
  return (
    <main className="auth-shell">
      <div className="auth-panel text-center">
        <p className="auth-brand">MyCareHub</p>
        <p className="mt-3 text-muted">
          Accedi al tuo spazio sanitario personale.
        </p>
        <div className="mt-8 flex flex-col gap-3">
          <Link href="/login" className="auth-button inline-flex items-center justify-center no-underline">
            Accedi
          </Link>
          <Link href="/registrazione" className="auth-link">
            Crea un account paziente
          </Link>
        </div>
      </div>
    </main>
  );
}
