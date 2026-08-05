-- Catalogo specializzazioni + audit trail admin
-- Applied remotely via Supabase MCP (admin_specializzazioni_audit_log)

CREATE TABLE IF NOT EXISTS public.specializzazioni (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  nome text NOT NULL,
  creato_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT specializzazioni_nome_unique UNIQUE (nome)
);

CREATE TABLE IF NOT EXISTS public.audit_log (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  attore_id uuid REFERENCES public.profili(id) ON DELETE SET NULL,
  attore_nome text,
  azione text NOT NULL,
  entita_tipo text,
  entita_id uuid,
  dettagli text,
  creato_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_audit_log_creato_at ON public.audit_log (creato_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_log_attore ON public.audit_log (attore_id);
CREATE INDEX IF NOT EXISTS idx_specializzazioni_nome ON public.specializzazioni (nome);

ALTER TABLE public.specializzazioni ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_log ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS specializzazioni_select_authenticated ON public.specializzazioni;
CREATE POLICY specializzazioni_select_authenticated
  ON public.specializzazioni
  FOR SELECT
  TO authenticated
  USING (true);

DROP POLICY IF EXISTS specializzazioni_all_service_role ON public.specializzazioni;
CREATE POLICY specializzazioni_all_service_role
  ON public.specializzazioni
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

DROP POLICY IF EXISTS audit_log_all_service_role ON public.audit_log;
CREATE POLICY audit_log_all_service_role
  ON public.audit_log
  FOR ALL
  TO service_role
  USING (true)
  WITH CHECK (true);

INSERT INTO public.specializzazioni (nome) VALUES
  ('Allergologia'),
  ('Anestesia e rianimazione'),
  ('Cardiologia'),
  ('Chirurgia generale'),
  ('Dermatologia'),
  ('Endocrinologia'),
  ('Gastroenterologia'),
  ('Geriatria'),
  ('Ginecologia'),
  ('Medicina interna'),
  ('Neurologia'),
  ('Oculistica'),
  ('Ortopedia'),
  ('Otorinolaringoiatria'),
  ('Pediatria'),
  ('Pneumologia'),
  ('Psichiatria'),
  ('Radiologia'),
  ('Reumatologia'),
  ('Urologia')
ON CONFLICT (nome) DO NOTHING;
