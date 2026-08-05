-- Chat real-time medico ↔ paziente: storico + realtime

CREATE TABLE IF NOT EXISTS public.conversazioni (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  paziente_id uuid NOT NULL REFERENCES public.profili(id) ON DELETE CASCADE,
  medico_id uuid NOT NULL REFERENCES public.profili(id) ON DELETE CASCADE,
  creato_at timestamptz NOT NULL DEFAULT now(),
  aggiornato_at timestamptz NOT NULL DEFAULT now(),
  ultimo_messaggio_at timestamptz NULL,
  CONSTRAINT conversazioni_paziente_medico_unique UNIQUE (paziente_id, medico_id),
  CONSTRAINT conversazioni_diversi_partecipanti CHECK (paziente_id <> medico_id)
);

CREATE TABLE IF NOT EXISTS public.messaggi (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  conversazione_id uuid NOT NULL REFERENCES public.conversazioni(id) ON DELETE CASCADE,
  mittente_id uuid NOT NULL REFERENCES public.profili(id) ON DELETE CASCADE,
  contenuto text NOT NULL,
  creato_at timestamptz NOT NULL DEFAULT now(),
  letto_at timestamptz NULL,
  CONSTRAINT messaggi_contenuto_non_vuoto CHECK (
    char_length(trim(contenuto)) > 0 AND char_length(contenuto) <= 4000
  )
);

CREATE INDEX IF NOT EXISTS idx_conversazioni_paziente ON public.conversazioni(paziente_id);
CREATE INDEX IF NOT EXISTS idx_conversazioni_medico ON public.conversazioni(medico_id);
CREATE INDEX IF NOT EXISTS idx_conversazioni_ultimo ON public.conversazioni(ultimo_messaggio_at DESC NULLS LAST);
CREATE INDEX IF NOT EXISTS idx_messaggi_conversazione_creato ON public.messaggi(conversazione_id, creato_at);

CREATE OR REPLACE FUNCTION public.touch_conversazione_on_messaggio()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  UPDATE public.conversazioni
  SET ultimo_messaggio_at = NEW.creato_at,
      aggiornato_at = NEW.creato_at
  WHERE id = NEW.conversazione_id;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_messaggi_touch_conversazione ON public.messaggi;
CREATE TRIGGER trg_messaggi_touch_conversazione
  AFTER INSERT ON public.messaggi
  FOR EACH ROW
  EXECUTE FUNCTION public.touch_conversazione_on_messaggio();

ALTER TABLE public.conversazioni ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.messaggi ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS conversazioni_select_partecipanti ON public.conversazioni;
CREATE POLICY conversazioni_select_partecipanti
  ON public.conversazioni FOR SELECT TO authenticated
  USING (
    public.is_current_user_admin()
    OR paziente_id = auth.uid()
    OR medico_id = auth.uid()
  );

DROP POLICY IF EXISTS conversazioni_insert_partecipanti ON public.conversazioni;
CREATE POLICY conversazioni_insert_partecipanti
  ON public.conversazioni FOR INSERT TO authenticated
  WITH CHECK (
    (
      paziente_id = auth.uid()
      AND EXISTS (
        SELECT 1 FROM public.profili p
        WHERE p.id = auth.uid() AND p.ruolo = 'paziente'
      )
      AND EXISTS (
        SELECT 1 FROM public.profili m
        WHERE m.id = medico_id AND m.ruolo = 'medico'
      )
    )
    OR (
      medico_id = auth.uid()
      AND EXISTS (
        SELECT 1 FROM public.profili m
        WHERE m.id = auth.uid() AND m.ruolo = 'medico'
      )
      AND EXISTS (
        SELECT 1 FROM public.profili p
        WHERE p.id = paziente_id AND p.ruolo = 'paziente'
      )
    )
  );

DROP POLICY IF EXISTS conversazioni_update_partecipanti ON public.conversazioni;
CREATE POLICY conversazioni_update_partecipanti
  ON public.conversazioni FOR UPDATE TO authenticated
  USING (
    public.is_current_user_admin()
    OR paziente_id = auth.uid()
    OR medico_id = auth.uid()
  )
  WITH CHECK (
    public.is_current_user_admin()
    OR paziente_id = auth.uid()
    OR medico_id = auth.uid()
  );

DROP POLICY IF EXISTS messaggi_select_partecipanti ON public.messaggi;
CREATE POLICY messaggi_select_partecipanti
  ON public.messaggi FOR SELECT TO authenticated
  USING (
    public.is_current_user_admin()
    OR EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  );

DROP POLICY IF EXISTS messaggi_insert_mittente ON public.messaggi;
CREATE POLICY messaggi_insert_mittente
  ON public.messaggi FOR INSERT TO authenticated
  WITH CHECK (
    mittente_id = auth.uid()
    AND EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  );

DROP POLICY IF EXISTS messaggi_update_partecipanti ON public.messaggi;
CREATE POLICY messaggi_update_partecipanti
  ON public.messaggi FOR UPDATE TO authenticated
  USING (
    public.is_current_user_admin()
    OR EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  )
  WITH CHECK (
    public.is_current_user_admin()
    OR EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  );

ALTER TABLE public.messaggi REPLICA IDENTITY FULL;
ALTER TABLE public.conversazioni REPLICA IDENTITY FULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime'
      AND schemaname = 'public'
      AND tablename = 'messaggi'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.messaggi;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_publication_tables
    WHERE pubname = 'supabase_realtime'
      AND schemaname = 'public'
      AND tablename = 'conversazioni'
  ) THEN
    ALTER PUBLICATION supabase_realtime ADD TABLE public.conversazioni;
  END IF;
END $$;
