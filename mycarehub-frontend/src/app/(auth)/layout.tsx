import type { ReactNode } from "react";

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <main className="auth-shell">
      <div className="auth-panel w-full max-w-xl">
        <div className="mb-8 text-center">
          <p className="auth-brand">MyCareHub</p>
          <p className="mt-2 text-sm text-muted">
            Accesso sicuro allo spazio clinico e anagrafico.
          </p>
        </div>
        {children}
      </div>
    </main>
  );
}
