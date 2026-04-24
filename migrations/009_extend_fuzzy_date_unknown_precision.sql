ALTER TABLE public.fuzzy_dates
    ALTER COLUMN date DROP NOT NULL,
    DROP CONSTRAINT IF EXISTS fuzzy_dates_precision_check,
    ADD CONSTRAINT fuzzy_dates_precision_check
        CHECK (precision IN ('exact', 'month', 'year', 'estimated', 'before', 'after', 'between', 'unknown'));
