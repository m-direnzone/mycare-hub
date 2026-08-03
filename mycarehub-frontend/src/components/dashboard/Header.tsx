"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { logout } from "@/src/lib/supabase/client";
import { useDashboard } from "./DashboardContext";

export function Header({ onMenuToggle }: { onMenuToggle: () => void }) {
  const router = useRouter();
  const { profilo, email } = useDashboard();
  const [open, setOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (!menuRef.current?.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, []);

  async function onLogout() {
    await logout();
    router.replace("/login");
  }

  const initials =
    `${profilo.nome?.[0] ?? ""}${profilo.cognome?.[0] ?? ""}`.toUpperCase() ||
    "U";

  return (
    <header className="dash-header">
      <div className="dash-header-left">
        <button
          type="button"
          className="dash-icon-btn dash-menu-btn"
          onClick={onMenuToggle}
          aria-label="Apri menu"
        >
          <span />
          <span />
          <span />
        </button>
        <p className="dash-header-title">Dashboard</p>
      </div>

      <div className="dash-header-right" ref={menuRef}>
        <button
          type="button"
          className="dash-avatar-btn"
          onClick={() => setOpen((v) => !v)}
          aria-expanded={open}
          aria-haspopup="menu"
        >
          <span className="dash-avatar">{initials}</span>
        </button>

        {open && (
          <div className="dash-profile-menu" role="menu">
            <div className="dash-profile-menu-info">
              <p className="dash-profile-name">
                {profilo.nome} {profilo.cognome}
              </p>
              <p className="dash-profile-meta">
                {email ?? "email non disponibile"}
              </p>
              <p className="dash-profile-meta">
                CF: {profilo.codiceFiscale || "—"} · ruolo {profilo.ruolo}
              </p>
            </div>
            <button
              type="button"
              className="dash-profile-logout"
              role="menuitem"
              onClick={onLogout}
            >
              Esci
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
