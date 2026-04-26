-- public.users (mit FK zu auth.users — gilt nur in Supabase, nicht lokal)
CREATE TABLE IF NOT EXISTS public.users (
    id         uuid        NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    first_name text        NOT NULL,
    last_name  text        NOT NULL,
    email      text        NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT users_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.boards (
    id         uuid        NOT NULL DEFAULT gen_random_uuid(),
    name       text        NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT boards_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.board_members (
    id                uuid        NOT NULL DEFAULT gen_random_uuid(),
    board_id          uuid        NOT NULL REFERENCES public.boards(id) ON DELETE CASCADE,
    user_id           uuid        NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    role              text        NOT NULL CHECK (role IN ('owner', 'editor', 'viewer')),
    viewer_privacy_mode text        CHECK (viewer_privacy_mode IN ('full', 'restricted')),
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT board_members_pkey PRIMARY KEY (id),
    CONSTRAINT board_members_unique UNIQUE (board_id, user_id)
);

CREATE TABLE IF NOT EXISTS public.fuzzy_dates (
    id                uuid        NOT NULL DEFAULT gen_random_uuid(),
    precision         text        NOT NULL CHECK (precision IN ('exact', 'month', 'year', 'estimated', 'before', 'after', 'between', 'unknown')),
    date              date,
    date_precision    text        CHECK (date_precision IN ('exact', 'month', 'year')),
    date_to           date,
    date_to_precision text        CHECK (date_to_precision IN ('exact', 'month', 'year')),
    note              text,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_fuzzy_dates PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS public.persons (
    id            uuid        NOT NULL DEFAULT gen_random_uuid(),
    board_id      uuid        NOT NULL REFERENCES public.boards(id) ON DELETE CASCADE,
    first_name    text        NOT NULL,
    last_name     text        NOT NULL,
    middle_names  text,
    birth_name    text,
    gender        text        CHECK (gender IN ('male', 'female', 'diverse')),
    birth_place   text,
    death_place   text,
    burial_place  text,
    title         text,
    religion      text,
    notes         text,
    created_at    timestamptz NOT NULL DEFAULT now(),
    birth_date_id uuid,
    death_date_id uuid,
    CONSTRAINT pk_persons PRIMARY KEY (id)
);

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

CREATE TABLE IF NOT EXISTS public.relations (
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

CREATE TABLE IF NOT EXISTS public.residences (
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
