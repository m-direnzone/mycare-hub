-- Soft delete specializzazioni + vincoli DB su cancellazioni
-- Applied remotely via Supabase MCP (soft_delete_and_delete_constraints)
-- Aggiornato: disabilitazione medico solo con prenotazioni confermate future

-- 1) Soft delete catalogo specializzazioni
ALTER TABLE public.specializzazioni
  ADD COLUMN IF NOT EXISTS deleted_at timestamptz NULL;

ALTER TABLE public.specializzazioni
  DROP CONSTRAINT IF EXISTS specializzazioni_nome_unique;

CREATE UNIQUE INDEX IF NOT EXISTS specializzazioni_nome_attive_unique
  ON public.specializzazioni (nome)
  WHERE deleted_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_specializzazioni_deleted_at
  ON public.specializzazioni (deleted_at);

DROP POLICY IF EXISTS specializzazioni_select_authenticated ON public.specializzazioni;
CREATE POLICY specializzazioni_select_authenticated
  ON public.specializzazioni
  FOR SELECT
  TO authenticated
  USING (deleted_at IS NULL OR public.is_current_user_admin());

DROP POLICY IF EXISTS specializzazioni_delete_admin ON public.specializzazioni;

DROP POLICY IF EXISTS specializzazioni_update_admin ON public.specializzazioni;
CREATE POLICY specializzazioni_update_admin
  ON public.specializzazioni
  FOR UPDATE
  TO authenticated
  USING (public.is_current_user_admin())
  WITH CHECK (public.is_current_user_admin());

-- 2) Impedisce disabilitazione medico con prenotazioni confermate future
--    (le prenotazioni in_attesa non bloccano)
CREATE OR REPLACE FUNCTION public.prevent_disable_medico_with_future_visits()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  IF OLD.attivo IS TRUE AND NEW.attivo IS FALSE THEN
    IF EXISTS (
      SELECT 1
      FROM public.prenotazioni p
      JOIN public.slot_disponibilita s ON s.id = p.slot_id
      WHERE p.medico_id = NEW.medico_id
        AND p.stato = 'confermata'::public.stato_prenotazione
        AND s.data_ora_inizio >= timezone('utc', now())
    ) THEN
      RAISE EXCEPTION
        'Impossibile disabilitare il medico: ha almeno una prenotazione confermata in futuro.'
        USING ERRCODE = 'P0001';
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_prevent_disable_medico_with_future_visits
  ON public.medici_dettagli;
CREATE TRIGGER trg_prevent_disable_medico_with_future_visits
  BEFORE UPDATE OF attivo ON public.medici_dettagli
  FOR EACH ROW
  EXECUTE FUNCTION public.prevent_disable_medico_with_future_visits();

-- 3) Soft delete specializzazione solo se non assegnata a medici attivi
CREATE OR REPLACE FUNCTION public.prevent_soft_delete_specializzazione_in_use()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  IF OLD.deleted_at IS NULL AND NEW.deleted_at IS NOT NULL THEN
    IF EXISTS (
      SELECT 1
      FROM public.medici_dettagli md
      WHERE md.attivo IS TRUE
        AND lower(md.specializzazione) = lower(OLD.nome)
    ) THEN
      RAISE EXCEPTION
        'Impossibile eliminare la specializzazione: è assegnata ad almeno un medico attivo.'
        USING ERRCODE = 'P0001';
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_prevent_soft_delete_specializzazione_in_use
  ON public.specializzazioni;
CREATE TRIGGER trg_prevent_soft_delete_specializzazione_in_use
  BEFORE UPDATE OF deleted_at ON public.specializzazioni
  FOR EACH ROW
  EXECUTE FUNCTION public.prevent_soft_delete_specializzazione_in_use();

-- 4) Blocca hard delete specializzazioni (solo soft delete)
CREATE OR REPLACE FUNCTION public.prevent_hard_delete_specializzazione()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  RAISE EXCEPTION
    'Hard delete non consentito sulle specializzazioni: usare soft delete (deleted_at).'
    USING ERRCODE = 'P0001';
END;
$$;

DROP TRIGGER IF EXISTS trg_prevent_hard_delete_specializzazione
  ON public.specializzazioni;
CREATE TRIGGER trg_prevent_hard_delete_specializzazione
  BEFORE DELETE ON public.specializzazioni
  FOR EACH ROW
  EXECUTE FUNCTION public.prevent_hard_delete_specializzazione();
