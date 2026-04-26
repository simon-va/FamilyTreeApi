ALTER TABLE public.residences
    ADD COLUMN IF NOT EXISTS lat                   double precision,
    ADD COLUMN IF NOT EXISTS lng                   double precision,
    ADD COLUMN IF NOT EXISTS moved_to_residence_id uuid REFERENCES public.residences(id) ON DELETE SET NULL;
