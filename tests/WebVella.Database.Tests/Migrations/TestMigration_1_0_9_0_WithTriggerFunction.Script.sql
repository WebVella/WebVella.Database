-- Migration 1.0.9.0: Creates a trigger function with IF and ELSIF blocks.
CREATE OR REPLACE FUNCTION fn_do_something()
RETURNS TRIGGER AS $$
BEGIN
	IF (TG_OP = 'INSERT') THEN
		PERFORM 1;
		RETURN NEW;
	ELSIF (TG_OP = 'DELETE') THEN
		PERFORM 1;
		RETURN OLD;
	END IF;

	RETURN NULL;
END
$$ LANGUAGE plpgsql;
