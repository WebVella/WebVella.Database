-- Migration 1.0.8.0: Creates a function that contains IF statements and multiple semicolons.
CREATE OR REPLACE FUNCTION test_migration_if_function()
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
	IF NOT EXISTS (
		SELECT 1
		FROM test_migration_table
		WHERE name = 'Function IF Item'
	) THEN
		INSERT INTO test_migration_table (name, description)
		VALUES ('Function IF Item', 'Created by a function with IF statements');
	END IF;
END;
$$;

SELECT test_migration_if_function();
