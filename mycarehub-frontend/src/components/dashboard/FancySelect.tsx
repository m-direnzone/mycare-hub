"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";

export type FancySelectOption = {
  value: string;
  label: string;
  hint?: string;
};

type FancySelectProps = {
  label: string;
  value: string;
  options: FancySelectOption[];
  placeholder?: string;
  disabled?: boolean;
  invalid?: boolean;
  onChange: (value: string) => void;
};

export function FancySelect({
  label,
  value,
  options,
  placeholder = "Seleziona…",
  disabled = false,
  invalid = false,
  onChange,
}: FancySelectProps) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const listId = useId();

  const selected = options.find((o) => o.value === value) ?? null;

  useEffect(() => {
    if (!open) return;

    function onPointerDown(e: MouseEvent) {
      if (!rootRef.current?.contains(e.target as Node)) {
        setOpen(false);
      }
    }

    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  useEffect(() => {
    if (disabled) setOpen(false);
  }, [disabled]);

  return (
    <label className="dash-label fancy-select-field">
      {label}
      <div
        className={`fancy-select${open ? " is-open" : ""}${disabled ? " is-disabled" : ""}${invalid ? " is-invalid" : ""}`}
        ref={rootRef}
      >
        <button
          type="button"
          className="fancy-select-trigger"
          disabled={disabled}
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={listId}
          onClick={() => setOpen((v) => !v)}
        >
          <span className={`fancy-select-value${selected ? "" : " is-placeholder"}`}>
            {selected ? (
              <>
                <span className="fancy-select-main">{selected.label}</span>
                {selected.hint ? (
                  <span className="fancy-select-hint">{selected.hint}</span>
                ) : null}
              </>
            ) : (
              placeholder
            )}
          </span>
          <span className="fancy-select-chevron" aria-hidden />
        </button>

        {open && !disabled ? (
          <ul
            id={listId}
            className="fancy-select-menu"
            role="listbox"
            aria-label={label}
          >
            {options.length === 0 ? (
              <li className="fancy-select-empty">Nessuna opzione</li>
            ) : (
              options.map((opt) => {
                const active = opt.value === value;
                return (
                  <li key={opt.value} role="option" aria-selected={active}>
                    <button
                      type="button"
                      className={`fancy-select-option${active ? " is-active" : ""}`}
                      onClick={() => {
                        onChange(opt.value);
                        setOpen(false);
                      }}
                    >
                      <span className="fancy-select-main">{opt.label}</span>
                      {opt.hint ? (
                        <span className="fancy-select-hint">{opt.hint}</span>
                      ) : null}
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        ) : null}
      </div>
    </label>
  );
}
