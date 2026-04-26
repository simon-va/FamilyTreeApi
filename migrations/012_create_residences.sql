CREATE TABLE public.residences (
    id            uuid        NOT NULL DEFAULT gen_random_uuid(),
    board_id      uuid        NOT NULL REFERENCES public.boards(id) ON DELETE CASCADE,
    person_id     uuid        NOT NULL REFERENCES public.persons(id) ON DELETE CASCADE,
    city          text,
    country       text,
    street        text,
    notes         text,
    start_date_id uuid,
    end_date_id   uuid,
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_residences PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION delete_residence_fuzzy_dates()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.start_date_id IS NOT NULL THEN
        DELETE FROM public.fuzzy_dates WHERE id = OLD.start_date_id;
    END IF;
    IF OLD.end_date_id IS NOT NULL THEN
        DELETE FROM public.fuzzy_dates WHERE id = OLD.end_date_id;
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_delete_residence_fuzzy_dates
BEFORE DELETE ON public.residences
FOR EACH ROW EXECUTE FUNCTION delete_residence_fuzzy_dates();
