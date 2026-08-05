-- Bucket privato per PDF referti clinici (Signed URL via backend)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('referti', 'referti', false, 10485760, ARRAY['application/pdf']::text[])
ON CONFLICT (id) DO UPDATE
SET
  public = EXCLUDED.public,
  file_size_limit = EXCLUDED.file_size_limit,
  allowed_mime_types = EXCLUDED.allowed_mime_types;

-- Path: {pazienteId}/{medicoId}/{filename}
DROP POLICY IF EXISTS "referti_insert_medico" ON storage.objects;
CREATE POLICY "referti_insert_medico"
  ON storage.objects FOR INSERT TO authenticated
  WITH CHECK (
    bucket_id = 'referti'
    AND (storage.foldername(name))[2] = auth.uid()::text
    AND EXISTS (
      SELECT 1 FROM public.profili p
      WHERE p.id = auth.uid() AND p.ruolo = 'medico'
    )
  );

DROP POLICY IF EXISTS "referti_select_partecipanti" ON storage.objects;
CREATE POLICY "referti_select_partecipanti"
  ON storage.objects FOR SELECT TO authenticated
  USING (
    bucket_id = 'referti'
    AND (
      public.is_current_user_admin()
      OR (storage.foldername(name))[1] = auth.uid()::text
      OR (storage.foldername(name))[2] = auth.uid()::text
    )
  );

DROP POLICY IF EXISTS "referti_delete_medico" ON storage.objects;
CREATE POLICY "referti_delete_medico"
  ON storage.objects FOR DELETE TO authenticated
  USING (
    bucket_id = 'referti'
    AND (
      public.is_current_user_admin()
      OR (
        (storage.foldername(name))[2] = auth.uid()::text
        AND EXISTS (
          SELECT 1 FROM public.profili p
          WHERE p.id = auth.uid() AND p.ruolo = 'medico'
        )
      )
    )
  );
