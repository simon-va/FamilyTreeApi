create table if not exists public.board_members (
    id                uuid        not null default gen_random_uuid(),
    board_id          uuid        not null references public.boards(id) on delete cascade,
    user_id           uuid        not null references public.users(id) on delete cascade,
    role              text        not null check (role in ('owner', 'editor', 'viewer')),
    privacy_overrides jsonb       not null default '{}',
    created_at        timestamptz not null default now(),

    constraint board_members_pkey primary key (id),
    constraint board_members_unique unique (board_id, user_id)
);
