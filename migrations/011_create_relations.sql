CREATE TABLE public.relations (
    id            uuid        NOT NULL DEFAULT gen_random_uuid(),
    board_id      uuid        NOT NULL REFERENCES public.boards(id) ON DELETE CASCADE,
    person_a_id   uuid        NOT NULL REFERENCES public.persons(id) ON DELETE CASCADE,
    person_b_id   uuid        NOT NULL REFERENCES public.persons(id) ON DELETE CASCADE,
    type          text        NOT NULL CHECK (type IN (
                                  'biological_parent', 'adoptive_parent', 'foster_parent',
                                  'spouse', 'partner', 'engaged')),
    start_date_id uuid,
    end_date_id   uuid,
    end_reason    text,
    notes         text,
    created_at    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_relations PRIMARY KEY (id)
);

CREATE OR REPLACE FUNCTION delete_relation_fuzzy_dates()
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

CREATE TRIGGER trigger_delete_relation_fuzzy_dates
BEFORE DELETE ON public.relations
FOR EACH ROW EXECUTE FUNCTION delete_relation_fuzzy_dates();
