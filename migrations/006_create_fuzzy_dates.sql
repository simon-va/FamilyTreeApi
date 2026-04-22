CREATE TABLE public.fuzzy_dates (
    id                uuid        not null default gen_random_uuid(),
    precision         text        not null check (precision in ('exact', 'month', 'year', 'estimated', 'before', 'after', 'between')),
    date              date        not null,
    date_precision    text        check (date_precision in ('exact', 'month', 'year')),
    date_to           date,
    date_to_precision text        check (date_to_precision in ('exact', 'month', 'year')),
    note              text,
    created_at        timestamptz not null default now(),

    constraint pk_fuzzy_dates primary key (id)
);
