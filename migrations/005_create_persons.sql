CREATE TABLE public.persons (
    id            uuid         not null default gen_random_uuid(),
    board_id      uuid         not null references public.boards(id) on delete cascade,
    first_name    text         not null,
    last_name     text         not null,
    middle_names  text,
    birth_name    text,
    gender        text         check (gender in ('male', 'female', 'diverse')),
    birth_place   text,
    death_place   text,
    burial_place  text,
    title         text,
    religion      text,
    notes         text,
    is_deleted    bool         not null default false,
    deleted_at    timestamptz,
    created_at    timestamptz  not null default now(),

    constraint pk_persons primary key (id)
);
