-- Migration 1.0.7.0: Inserts a row via an explicit ScriptPath resource reference
INSERT INTO test_migration_table (name, description) VALUES ('Script Path Item', 'Added by explicit script path');
