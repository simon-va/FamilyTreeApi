-- public.users (ohne FK zu auth.users — der gilt nur in Supabase)
CREATE TABLE IF NOT EXISTS public.users (
    id         uuid        NOT NULL,
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
