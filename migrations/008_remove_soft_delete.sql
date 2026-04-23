ALTER TABLE public.boards
    DROP COLUMN is_deleted,
    DROP COLUMN deleted_at;

ALTER TABLE public.persons
    DROP COLUMN is_deleted,
    DROP COLUMN deleted_at;

CREATE OR REPLACE FUNCTION delete_person_fuzzy_dates()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.birth_date_id IS NOT NULL THEN
        DELETE FROM public.fuzzy_dates WHERE id = OLD.birth_date_id;
    END IF;
    IF OLD.death_date_id IS NOT NULL THEN
        DELETE FROM public.fuzzy_dates WHERE id = OLD.death_date_id;
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_delete_person_fuzzy_dates
BEFORE DELETE ON public.persons
FOR EACH ROW EXECUTE FUNCTION delete_person_fuzzy_dates();
