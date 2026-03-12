-- Migration 1.0.5.0: Adds embedded_source column to test loading SQL from embedded resources
ALTER TABLE test_migration_table ADD COLUMN IF NOT EXISTS embedded_source VARCHAR(100);
INSERT INTO test_migration_table (name, description, embedded_source) 
VALUES ('Embedded Resource Item', 'Added by embedded SQL script', 'embedded');
