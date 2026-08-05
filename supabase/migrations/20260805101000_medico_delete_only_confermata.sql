-- Vincolo disabilitazione medico: solo prenotazioni confermate future
-- (le prenotazioni in_attesa non bloccano)

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
