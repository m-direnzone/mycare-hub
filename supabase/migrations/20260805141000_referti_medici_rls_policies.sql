-- RLS su referti_medici: allineato al flusso backend (JWT utente + anon key)
ALTER TABLE public.referti_medici ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS referti_medici_insert_medico ON public.referti_medici;
CREATE POLICY referti_medici_insert_medico
  ON public.referti_medici FOR INSERT TO authenticated
  WITH CHECK (
    medico_id = auth.uid()
    AND EXISTS (
      SELECT 1 FROM public.profili p
      WHERE p.id = auth.uid() AND p.ruolo = 'medico'
    )
  );

DROP POLICY IF EXISTS referti_medici_select_partecipanti ON public.referti_medici;
CREATE POLICY referti_medici_select_partecipanti
  ON public.referti_medici FOR SELECT TO authenticated
  USING (
    public.is_current_user_admin()
    OR paziente_id = auth.uid()
    OR medico_id = auth.uid()
  );

DROP POLICY IF EXISTS referti_medici_delete_medico ON public.referti_medici;
CREATE POLICY referti_medici_delete_medico
  ON public.referti_medici FOR DELETE TO authenticated
  USING (
    public.is_current_user_admin()
    OR (
      medico_id = auth.uid()
      AND EXISTS (
        SELECT 1 FROM public.profili p
        WHERE p.id = auth.uid() AND p.ruolo = 'medico'
      )
    )
  );
