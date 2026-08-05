"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { fetchConversazioniApi } from "@/src/lib/chat-api";
import { navForRuolo } from "@/src/lib/nav";
import { supabase } from "@/src/lib/supabase/client";
import type { RuoloUtente } from "@/src/types/enums";

export function Sidebar({
  ruolo,
  open,
  onClose,
}: {
  ruolo: RuoloUtente;
  open: boolean;
  onClose: () => void;
}) {
  const pathname = usePathname();
  const items = navForRuolo(ruolo);
  const [hasUnreadChat, setHasUnreadChat] = useState(false);

  useEffect(() => {
    if (ruolo !== "paziente" && ruolo !== "medico") {
      setHasUnreadChat(false);
      return;
    }

    let cancelled = false;

    async function refreshUnread() {
      try {
        const conversazioni = await fetchConversazioniApi();
        if (cancelled) return;
        setHasUnreadChat(conversazioni.some((c) => c.messaggiNonLetti > 0));
      } catch {
        if (!cancelled) setHasUnreadChat(false);
      }
    }

    void refreshUnread();

    const channel = supabase
      .channel("sidebar-chat-unread")
      .on(
        "postgres_changes",
        {
          event: "*",
          schema: "public",
          table: "messaggi",
        },
        () => {
          void refreshUnread();
        }
      )
      .subscribe();

    const onFocus = () => {
      void refreshUnread();
    };
    window.addEventListener("focus", onFocus);

    return () => {
      cancelled = true;
      window.removeEventListener("focus", onFocus);
      void supabase.removeChannel(channel);
    };
  }, [ruolo, pathname]);

  const sections = items.reduce<Record<string, typeof items>>((acc, item) => {
    const key = item.section ?? "Menu";
    (acc[key] ??= []).push(item);
    return acc;
  }, {});

  return (
    <>
      <div
        className={`dash-sidebar-backdrop ${open ? "is-open" : ""}`}
        onClick={onClose}
        aria-hidden={!open}
      />
      <aside className={`dash-sidebar ${open ? "is-open" : ""}`}>
        <div className="dash-sidebar-brand">
          <Link
            href={ruolo === "admin" ? "/admin" : "/dashboard"}
            className="dash-logo"
            onClick={onClose}
          >
            MyCareHub
          </Link>
          <p className="dash-sidebar-role">
            {ruolo === "paziente"
              ? "Area paziente"
              : ruolo === "medico"
                ? "Area medico"
                : "Area amministrazione"}
          </p>
        </div>

        <nav className="dash-nav" aria-label="Navigazione dashboard">
          {Object.entries(sections).map(([section, sectionItems]) => (
            <div key={section} className="dash-nav-section">
              <p className="dash-nav-section-label">{section}</p>
              <ul>
                {sectionItems.map((item) => {
                  const matches = (href: string) => {
                    if (href === "/admin" || href === "/dashboard") {
                      return pathname === href;
                    }
                    return (
                      pathname === href || pathname.startsWith(`${href}/`)
                    );
                  };

                  const active =
                    matches(item.href) &&
                    !items.some(
                      (other) =>
                        other.href !== item.href &&
                        other.href.length > item.href.length &&
                        matches(other.href)
                    );

                  const showUnreadDot =
                    item.href === "/dashboard/chat" && hasUnreadChat;

                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        className={`dash-nav-link ${active ? "is-active" : ""}`}
                        onClick={onClose}
                      >
                        <span className="dash-nav-link-label">{item.label}</span>
                        {showUnreadDot ? (
                          <span
                            className="dash-nav-unread-dot"
                            aria-label="Messaggi non letti"
                            title="Messaggi non letti"
                          />
                        ) : null}
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </nav>
      </aside>
    </>
  );
}
