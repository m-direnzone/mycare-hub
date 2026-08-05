"use client";

import type { ReactNode } from "react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <main className="auth-shell">
      <div className="auth-panel w-full">
        <div className="mb-8 text-center">
          <p className="auth-brand">MyCareHub</p>
          <p className="mt-2 text-sm text-muted">
            Accesso sicuro allo spazio clinico e anagrafico.
          </p>
        </div>
        {children}
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
    </main>
  );
}
