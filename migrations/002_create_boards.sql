create table if not exists public.boards (
    id          uuid        not null default gen_random_uuid(),
    name        text        not null,
    is_deleted  bool        not null default false,
    deleted_at  timestamptz,
    created_at  timestamptz not null default now(),

    constraint boards_pkey primary key (id)
);
