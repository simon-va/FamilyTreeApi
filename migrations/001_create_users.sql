create table if not exists public.users (
    id          uuid        not null,
    first_name  text        not null,
    last_name   text        not null,
    created_at  timestamptz not null default now(),

    constraint users_pkey primary key (id),
    constraint users_id_fkey foreign key (id)
        references auth.users (id) on delete cascade
);

alter table public.users enable row level security;

create policy "Users can read their own row"
    on public.users
    for select
    using (auth.uid() = id);
