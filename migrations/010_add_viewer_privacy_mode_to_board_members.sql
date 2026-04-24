ALTER TABLE public.board_members
    DROP COLUMN privacy_overrides,
    ADD COLUMN viewer_privacy_mode text
        CHECK (viewer_privacy_mode IN ('full', 'restricted'));
