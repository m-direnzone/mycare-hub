-- Flag attivazione medico (password cambiata dopo primo accesso)
ALTER TABLE public.medici_dettagli
  ADD COLUMN IF NOT EXISTS password_cambiata boolean NOT NULL DEFAULT false;

UPDATE public.medici_dettagli md
SET password_cambiata = COALESCE(
  NOT ((au.raw_user_meta_data->>'must_change_password')::boolean),
  true
)
FROM auth.users au
WHERE au.id = md.medico_id;
