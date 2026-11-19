-- RLS è per riga, non per colonna (Supabase: column-level privileges).
-- Gli update legittimi restano quelli già usati dall'app:
--   conversazioni: ultimo_messaggio_at, aggiornato_at
--   messaggi: letto_at, e solo sui messaggi ricevuti
-- L'apertura di una conversazione richiede una prenotazione non cancellata,
-- la stessa condizione di ChatService.HannoPrenotazioneInComuneAsync.

CREATE SCHEMA IF NOT EXISTS private;

REVOKE ALL ON SCHEMA private FROM PUBLIC;
GRANT USAGE ON SCHEMA private TO authenticated;

CREATE OR REPLACE FUNCTION private.chat_hanno_prenotazione_condivisa(
  p_paziente uuid,
  p_medico uuid
)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.prenotazioni pr
    WHERE pr.paziente_id = p_paziente
      AND pr.medico_id = p_medico
      AND pr.stato <> 'cancellata'::public.stato_prenotazione
  );
$$;

REVOKE ALL ON FUNCTION private.chat_hanno_prenotazione_condivisa(uuid, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION private.chat_hanno_prenotazione_condivisa(uuid, uuid) TO authenticated;

CREATE OR REPLACE FUNCTION private.conversazioni_colonne_immutabili()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
  IF NEW.id IS DISTINCT FROM OLD.id
     OR NEW.paziente_id IS DISTINCT FROM OLD.paziente_id
     OR NEW.medico_id IS DISTINCT FROM OLD.medico_id
     OR NEW.creato_at IS DISTINCT FROM OLD.creato_at
  THEN
    RAISE EXCEPTION 'Aggiornamento non consentito su questa conversazione.'
      USING ERRCODE = '42501';
  END IF;

  RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION private.messaggi_solo_letto_at_destinatario()
RETURNS trigger
LANGUAGE plpgsql
SET search_path = public
AS $$
BEGIN
  IF NEW.id IS DISTINCT FROM OLD.id
     OR NEW.conversazione_id IS DISTINCT FROM OLD.conversazione_id
     OR NEW.mittente_id IS DISTINCT FROM OLD.mittente_id
     OR NEW.contenuto IS DISTINCT FROM OLD.contenuto
     OR NEW.creato_at IS DISTINCT FROM OLD.creato_at
  THEN
    RAISE EXCEPTION 'Su un messaggio si può aggiornare solo letto_at.'
      USING ERRCODE = '42501';
  END IF;

  IF NEW.letto_at IS DISTINCT FROM OLD.letto_at
     AND (
       auth.uid() IS NULL
       OR OLD.mittente_id = auth.uid()
     )
  THEN
    RAISE EXCEPTION 'Solo il destinatario può aggiornare letto_at.'
      USING ERRCODE = '42501';
  END IF;

  RETURN NEW;
END;
$$;

REVOKE ALL ON FUNCTION private.conversazioni_colonne_immutabili() FROM PUBLIC;
REVOKE ALL ON FUNCTION private.messaggi_solo_letto_at_destinatario() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION private.conversazioni_colonne_immutabili() TO authenticated;
GRANT EXECUTE ON FUNCTION private.messaggi_solo_letto_at_destinatario() TO authenticated;

DROP TRIGGER IF EXISTS trg_conversazioni_colonne_immutabili ON public.conversazioni;
CREATE TRIGGER trg_conversazioni_colonne_immutabili
  BEFORE UPDATE ON public.conversazioni
  FOR EACH ROW
  EXECUTE FUNCTION private.conversazioni_colonne_immutabili();

DROP TRIGGER IF EXISTS trg_messaggi_solo_letto_at ON public.messaggi;
CREATE TRIGGER trg_messaggi_solo_letto_at
  BEFORE UPDATE ON public.messaggi
  FOR EACH ROW
  EXECUTE FUNCTION private.messaggi_solo_letto_at_destinatario();

REVOKE UPDATE ON TABLE public.conversazioni FROM PUBLIC, anon, authenticated;
GRANT UPDATE (ultimo_messaggio_at, aggiornato_at)
  ON TABLE public.conversazioni TO authenticated;

REVOKE UPDATE ON TABLE public.messaggi FROM PUBLIC, anon, authenticated;
GRANT UPDATE (letto_at) ON TABLE public.messaggi TO authenticated;

DROP POLICY IF EXISTS conversazioni_insert_partecipanti ON public.conversazioni;
CREATE POLICY conversazioni_insert_partecipanti
  ON public.conversazioni FOR INSERT TO authenticated
  WITH CHECK (
    private.chat_hanno_prenotazione_condivisa(paziente_id, medico_id)
    AND (
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
    )
  );

DROP POLICY IF EXISTS messaggi_update_partecipanti ON public.messaggi;
DROP POLICY IF EXISTS messaggi_update_destinatario ON public.messaggi;
CREATE POLICY messaggi_update_destinatario
  ON public.messaggi FOR UPDATE TO authenticated
  USING (
    mittente_id <> auth.uid()
    AND EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  )
  WITH CHECK (
    mittente_id <> auth.uid()
    AND EXISTS (
      SELECT 1 FROM public.conversazioni c
      WHERE c.id = conversazione_id
        AND (c.paziente_id = auth.uid() OR c.medico_id = auth.uid())
    )
  );
