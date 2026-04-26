-- =============================================
-- Seed Script — lokale Entwicklung
-- Schritt 1: Ersetze die user_id unten durch
--            eine echte UUID aus auth.users.
-- =============================================

DO $$
DECLARE
    user_id       uuid := 'cd1f049c-754b-4569-a27d-d6bd907f9681'; -- <- hier echte userId aus dem auth Schema einsetzen
    board_id      uuid := gen_random_uuid();
    p1_birth_id   uuid := gen_random_uuid();
    p1_death_id   uuid := gen_random_uuid();
    p2_birth_id   uuid := gen_random_uuid();
    p2_death_id   uuid := gen_random_uuid();
BEGIN

    -- User
    INSERT INTO public.users (id, first_name, last_name, email)
    VALUES (user_id, 'Max', 'Mustermann', 'max.mustermann@example.com');

    -- Board
    INSERT INTO public.boards (id, name)
    VALUES (board_id, 'Familie Mustermann');

    -- Board member (owner)
    INSERT INTO public.board_members (board_id, user_id, role)
    VALUES (board_id, user_id, 0);

    -- Fuzzy dates — Person 1
    INSERT INTO public.fuzzy_dates (id, precision, date)
    VALUES
        (p1_birth_id, 2, '1920-01-01'),
        (p1_death_id, 2, '1985-01-01');

    -- Fuzzy dates — Person 2
    INSERT INTO public.fuzzy_dates (id, precision, date)
    VALUES
        (p2_birth_id, 0, '1923-04-15'),
        (p2_death_id, 0, '1990-11-03');

    -- Person 1
    INSERT INTO public.persons (board_id, first_name, last_name, gender, birth_date_id, death_date_id)
    VALUES (board_id, 'Hans', 'Mustermann', 0, p1_birth_id, p1_death_id);

    -- Person 2
    INSERT INTO public.persons (board_id, first_name, last_name, gender, birth_date_id, death_date_id)
    VALUES (board_id, 'Maria', 'Mustermann', 1, p2_birth_id, p2_death_id);

END $$;
