"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useDashboard } from "@/src/components/dashboard/DashboardContext";
import { ApiError } from "@/src/lib/api";
import {
  apriConversazioneApi,
  fetchChatContattiApi,
  fetchConversazioniApi,
  fetchMessaggiApi,
  inviaMessaggioApi,
  segnaMessaggiLettiApi,
} from "@/src/lib/chat-api";
import { formatDateTime } from "@/src/lib/dashboard-api";
import { supabase } from "@/src/lib/supabase/client";
import type {
  ChatContattoDto,
  ConversazioneDto,
  MessaggioDto,
} from "@/src/types/dto";
import { toast } from "react-toastify";

const PREVIEW_MAX = 42;

function truncatePreview(text: string | null | undefined, max = PREVIEW_MAX) {
  if (!text) return "";
  const compact = text.replace(/\s+/g, " ").trim();
  if (compact.length <= max) return compact;
  return `${compact.slice(0, Math.max(0, max - 1)).trimEnd()}…`;
}

function ChatPageContent() {
  const { profilo } = useDashboard();
  const searchParams = useSearchParams();
  const medicoIdParam = searchParams.get("medicoId");
  const mioId = profilo?.id ?? null;
  const [conversazioni, setConversazioni] = useState<ConversazioneDto[]>([]);
  const [contatti, setContatti] = useState<ChatContattoDto[]>([]);
  const [messaggi, setMessaggi] = useState<MessaggioDto[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [draft, setDraft] = useState("");
  const [loadingList, setLoadingList] = useState(true);
  const [loadingThread, setLoadingThread] = useState(false);
  const [sending, setSending] = useState(false);
  const [showNuova, setShowNuova] = useState(false);
  const [openingContatto, setOpeningContatto] = useState(false);
  const bottomRef = useRef<HTMLDivElement | null>(null);
  const selectedIdRef = useRef<string | null>(null);
  const mioIdRef = useRef<string | null>(null);
  const openedMedicoRef = useRef<string | null>(null);

  useEffect(() => {
    selectedIdRef.current = selectedId;
  }, [selectedId]);

  useEffect(() => {
    mioIdRef.current = mioId;
  }, [mioId]);

  const selected = conversazioni.find((c) => c.id === selectedId) ?? null;

  async function loadListe() {
    setLoadingList(true);
    try {
      const [conv, cont] = await Promise.all([
        fetchConversazioniApi(),
        fetchChatContattiApi(),
      ]);
      setConversazioni(conv);
      setContatti(cont);
      return { conv, cont };
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Impossibile caricare la chat."
      );
      return { conv: [] as ConversazioneDto[], cont: [] as ChatContattoDto[] };
    } finally {
      setLoadingList(false);
    }
  }

  useEffect(() => {
    void loadListe();
  }, []);

  useEffect(() => {
    if (!medicoIdParam) {
      openedMedicoRef.current = null;
      return;
    }
    if (openedMedicoRef.current === medicoIdParam) return;

    let cancelled = false;
    openedMedicoRef.current = medicoIdParam;
    setOpeningContatto(true);

    (async () => {
      try {
        const { conv } = await loadListe();
        if (cancelled) return;

        const esistente = conv.find((c) => c.medicoId === medicoIdParam);
        if (esistente) {
          setSelectedId(esistente.id);
          return;
        }

        const aperta = await apriConversazioneApi(medicoIdParam);
        if (cancelled) return;

        setConversazioni((prev) => {
          if (prev.some((c) => c.id === aperta.id)) return prev;
          return [aperta, ...prev];
        });
        setSelectedId(aperta.id);
        await loadListe();
      } catch (err) {
        if (!cancelled) {
          openedMedicoRef.current = null;
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile aprire la chat con il medico."
          );
        }
      } finally {
        if (!cancelled) setOpeningContatto(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [medicoIdParam]);

  useEffect(() => {
    if (!selectedId) {
      setMessaggi([]);
      return;
    }

    let cancelled = false;

    (async () => {
      setLoadingThread(true);
      try {
        const data = await fetchMessaggiApi(selectedId);
        if (cancelled) return;
        setMessaggi(data);
        await segnaMessaggiLettiApi(selectedId);
        setConversazioni((prev) =>
          prev.map((c) =>
            c.id === selectedId ? { ...c, messaggiNonLetti: 0 } : c
          )
        );
      } catch (err) {
        if (!cancelled) {
          toast.error(
            err instanceof ApiError
              ? err.message
              : "Impossibile caricare la conversazione."
          );
        }
      } finally {
        if (!cancelled) setLoadingThread(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [selectedId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messaggi, selectedId]);

  useEffect(() => {
    const channel = supabase
      .channel("chat-messaggi-realtime")
      .on(
        "postgres_changes",
        {
          event: "INSERT",
          schema: "public",
          table: "messaggi",
        },
        (payload) => {
          const row = payload.new as {
            id: string;
            conversazione_id: string;
            mittente_id: string;
            contenuto: string;
            creato_at: string;
            letto_at: string | null;
          };

          const activeId = selectedIdRef.current;
          const me = mioIdRef.current;
          const isMio = !!me && row.mittente_id === me;

          if (activeId && row.conversazione_id === activeId) {
            setMessaggi((prev) => {
              if (prev.some((m) => m.id === row.id)) return prev;
              return [
                ...prev,
                {
                  id: row.id,
                  conversazioneId: row.conversazione_id,
                  mittenteId: row.mittente_id,
                  contenuto: row.contenuto,
                  creatoAt: row.creato_at,
                  lettoAt: row.letto_at,
                  isMio,
                },
              ];
            });
            if (!isMio) {
              void segnaMessaggiLettiApi(activeId);
            }
          }

          void loadListe();
        }
      )
      .subscribe();

    return () => {
      void supabase.removeChannel(channel);
    };
  }, []);

  async function onApriContatto(contatto: ChatContattoDto) {
    try {
      if (contatto.conversazioneId) {
        setSelectedId(contatto.conversazioneId);
        setShowNuova(false);
        return;
      }

      const conv = await apriConversazioneApi(contatto.id);
      setConversazioni((prev) => {
        if (prev.some((c) => c.id === conv.id)) return prev;
        return [conv, ...prev];
      });
      setSelectedId(conv.id);
      setShowNuova(false);
      await loadListe();
    } catch (err) {
      toast.error(
        err instanceof ApiError
          ? err.message
          : "Impossibile aprire la conversazione."
      );
    }
  }

  async function onInvia(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedId || !draft.trim() || sending) return;

    const testo = draft.trim();
    setSending(true);
    setDraft("");
    try {
      const msg = await inviaMessaggioApi(selectedId, testo);
      setMessaggi((prev) => {
        if (prev.some((m) => m.id === msg.id)) return prev;
        return [...prev, msg];
      });
      setConversazioni((prev) =>
        prev
          .map((c) =>
            c.id === selectedId
              ? {
                  ...c,
                  anteprimaUltimoMessaggio: msg.contenuto,
                  ultimoMessaggioAt: msg.creatoAt,
                }
              : c
          )
          .sort((a, b) => {
            const ta = a.ultimoMessaggioAt
              ? new Date(a.ultimoMessaggioAt).getTime()
              : 0;
            const tb = b.ultimoMessaggioAt
              ? new Date(b.ultimoMessaggioAt).getTime()
              : 0;
            return tb - ta;
          })
      );
    } catch (err) {
      setDraft(testo);
      toast.error(
        err instanceof ApiError ? err.message : "Invio non riuscito."
      );
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="dash-page chat-page">
      <header className="dash-page-header">
        <div className="dash-page-header-row">
          <div>
            <h1 className="dash-page-title">Chat</h1>
            <p className="dash-page-subtitle">
              Messaggi in tempo reale tra medico e paziente.
            </p>
          </div>
          <button
            type="button"
            className="dash-btn"
            onClick={() => setShowNuova((v) => !v)}
          >
            {showNuova ? "Chiudi" : "Nuova chat"}
          </button>
        </div>
      </header>

      <div className="chat-layout">
        <aside className="chat-sidebar dash-panel">
          {showNuova ? (
            <div className="chat-sidebar-section">
              <p className="chat-section-label">Contatti disponibili</p>
              {loadingList ? (
                <p className="chat-hint">Caricamento…</p>
              ) : contatti.length === 0 ? (
                <p className="chat-hint">
                  Nessun contatto: serve almeno una prenotazione attiva con
                  l&apos;altra parte.
                </p>
              ) : (
                <ul className="chat-list">
                  {contatti.map((c) => (
                    <li key={c.id}>
                      <button
                        type="button"
                        className="chat-list-item"
                        onClick={() => void onApriContatto(c)}
                      >
                        <span className="chat-list-name">{c.nomeCompleto}</span>
                        {c.sottoTitolo ? (
                          <span className="chat-list-meta">{c.sottoTitolo}</span>
                        ) : null}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ) : null}

          <div className="chat-sidebar-section">
            <p className="chat-section-label">Conversazioni</p>
            {loadingList || openingContatto ? (
              <p className="chat-hint">Caricamento…</p>
            ) : conversazioni.length === 0 ? (
              <p className="chat-hint">
                Nessuna conversazione. Avvia una nuova chat da un contatto.
              </p>
            ) : (
              <ul className="chat-list">
                {conversazioni.map((c) => (
                  <li key={c.id}>
                    <button
                      type="button"
                      className={`chat-list-item${
                        selectedId === c.id ? " is-active" : ""
                      }`}
                      onClick={() => setSelectedId(c.id)}
                    >
                      <span className="chat-list-row">
                        <span className="chat-list-name">
                          {c.contattoNomeCompleto}
                        </span>
                        {c.messaggiNonLetti > 0 ? (
                          <span className="chat-unread">
                            {c.messaggiNonLetti}
                          </span>
                        ) : null}
                      </span>
                      {c.contattoSottoTitolo ? (
                        <span className="chat-list-meta">
                          {c.contattoSottoTitolo}
                        </span>
                      ) : null}
                      {c.anteprimaUltimoMessaggio ? (
                        <span
                          className="chat-list-preview"
                          title={c.anteprimaUltimoMessaggio}
                        >
                          {truncatePreview(c.anteprimaUltimoMessaggio)}
                        </span>
                      ) : null}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </aside>

        <section className="chat-thread dash-panel">
          {!selected ? (
            <div className="chat-empty">
              <p>
                {openingContatto
                  ? "Apertura chat…"
                  : "Seleziona una conversazione o avvia una nuova chat."}
              </p>
            </div>
          ) : (
            <>
              <header className="chat-thread-header">
                <div>
                  <p className="chat-thread-title">
                    {selected.contattoNomeCompleto}
                  </p>
                  {selected.contattoSottoTitolo ? (
                    <p className="chat-thread-meta">
                      {selected.contattoSottoTitolo}
                    </p>
                  ) : null}
                </div>
              </header>

              <div className="chat-messages">
                {loadingThread ? (
                  <p className="chat-hint">Caricamento messaggi…</p>
                ) : messaggi.length === 0 ? (
                  <p className="chat-hint chat-empty-thread">
                    Nessun messaggio ancora. Scrivi il primo.
                  </p>
                ) : (
                  messaggi.map((m) => (
                    <div
                      key={m.id}
                      className={`chat-bubble${m.isMio ? " is-mine" : ""}`}
                    >
                      <p className="chat-bubble-text">{m.contenuto}</p>
                      <time className="chat-bubble-time" dateTime={m.creatoAt}>
                        {formatDateTime(m.creatoAt)}
                      </time>
                    </div>
                  ))
                )}
                <div ref={bottomRef} />
              </div>

              <form className="chat-composer" onSubmit={onInvia}>
                <label className="sr-only" htmlFor="chat-draft">
                  Messaggio
                </label>
                <textarea
                  id="chat-draft"
                  className="chat-input"
                  rows={2}
                  maxLength={4000}
                  placeholder="Scrivi un messaggio…"
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      void onInvia(e);
                    }
                  }}
                  disabled={sending}
                />
                <button
                  type="submit"
                  className="dash-btn"
                  disabled={sending || !draft.trim()}
                >
                  {sending ? "Invio…" : "Invia"}
                </button>
              </form>
            </>
          )}
        </section>
      </div>
    </div>
  );
}

export default function ChatPage() {
  return (
    <Suspense fallback={<p className="chat-hint">Caricamento chat…</p>}>
      <ChatPageContent />
    </Suspense>
  );
}
