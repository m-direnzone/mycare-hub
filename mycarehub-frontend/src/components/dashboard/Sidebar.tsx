"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { navForRuolo } from "@/src/lib/nav";
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
          <Link href="/dashboard" className="dash-logo" onClick={onClose}>
            MyCareHub
          </Link>
          <p className="dash-sidebar-role">
            {ruolo === "paziente" ? "Area paziente" : "Area medico"}
          </p>
        </div>

        <nav className="dash-nav" aria-label="Navigazione dashboard">
          {Object.entries(sections).map(([section, sectionItems]) => (
            <div key={section} className="dash-nav-section">
              <p className="dash-nav-section-label">{section}</p>
              <ul>
                {sectionItems.map((item) => {
                  const matches = (href: string) =>
                    pathname === href ||
                    (href !== "/dashboard" &&
                      pathname.startsWith(`${href}/`));

                  const active =
                    matches(item.href) &&
                    !items.some(
                      (other) =>
                        other.href !== item.href &&
                        other.href.length > item.href.length &&
                        matches(other.href)
                    );

                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        className={`dash-nav-link ${active ? "is-active" : ""}`}
                        onClick={onClose}
                      >
                        {item.label}
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
