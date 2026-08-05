-- Admin JWT: accesso operativo senza service role nel backend ASP.NET

DROP POLICY IF EXISTS medici_dettagli_select_admin ON public.medici_dettagli;
CREATE POLICY medici_dettagli_select_admin
  ON public.medici_dettagli FOR SELECT TO authenticated
  USING (public.is_current_user_admin());

DROP POLICY IF EXISTS medici_dettagli_update_admin ON public.medici_dettagli;
CREATE POLICY medici_dettagli_update_admin
  ON public.medici_dettagli FOR UPDATE TO authenticated
  USING (public.is_current_user_admin())
  WITH CHECK (public.is_current_user_admin());

DROP POLICY IF EXISTS medici_dettagli_update_own ON public.medici_dettagli;
CREATE POLICY medici_dettagli_update_own
  ON public.medici_dettagli FOR UPDATE TO authenticated
  USING (medico_id = auth.uid())
  WITH CHECK (medico_id = auth.uid());

DROP POLICY IF EXISTS prenotazioni_select_admin ON public.prenotazioni;
CREATE POLICY prenotazioni_select_admin
  ON public.prenotazioni FOR SELECT TO authenticated
  USING (public.is_current_user_admin());

DROP POLICY IF EXISTS audit_log_select_admin ON public.audit_log;
CREATE POLICY audit_log_select_admin
  ON public.audit_log FOR SELECT TO authenticated
  USING (public.is_current_user_admin());

DROP POLICY IF EXISTS audit_log_insert_admin ON public.audit_log;
CREATE POLICY audit_log_insert_admin
  ON public.audit_log FOR INSERT TO authenticated
  WITH CHECK (public.is_current_user_admin());

DROP POLICY IF EXISTS specializzazioni_insert_admin ON public.specializzazioni;
CREATE POLICY specializzazioni_insert_admin
  ON public.specializzazioni FOR INSERT TO authenticated
  WITH CHECK (public.is_current_user_admin());

DROP POLICY IF EXISTS specializzazioni_delete_admin ON public.specializzazioni;
CREATE POLICY specializzazioni_delete_admin
  ON public.specializzazioni FOR DELETE TO authenticated
  USING (public.is_current_user_admin());
