-- ============================================================================
-- ETF TRACKER - DATA EXPORT FOR RENDER.COM IMPORT
-- ============================================================================
-- This SQL file exports all your data from the local database.
-- Use this to import your data into your Render.com PostgreSQL database.
--
-- Generated: April 3, 2026
-- ============================================================================

-- STEP 1: Export all users data
-- ============================================================================
-- NOTE: Replace the data below with your actual users from the command:
-- SELECT * FROM users;

INSERT INTO users (id, email, first_name, last_name, created_at, updated_at)
VALUES
-- EXAMPLE: (1, 'user@example.com', 'John', 'Doe', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
-- Replace with your actual data from: SELECT * FROM users;
;


-- STEP 2: Export all holdings data
-- ============================================================================
-- NOTE: Replace the data below with your actual holdings from the command:
-- SELECT * FROM holdings;

INSERT INTO holdings (id, user_id, ticker, etf_name, quantity, average_cost, broker, price_source, created_at, updated_at)
VALUES
-- EXAMPLE: (1, 1, 'VWRL.L', 'Vanguard FTSE All-World', 10.5, 85.50, 'Interactive Brokers', 'Eodhd', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
-- Replace with your actual data from: SELECT * FROM holdings;
;


-- STEP 3: Export all transactions data
-- ============================================================================
-- NOTE: Replace the data below with your actual transactions from the command:
-- SELECT * FROM transactions;

INSERT INTO transactions (id, holding_id, quantity, purchase_price, purchase_date, created_at, updated_at)
VALUES
-- EXAMPLE: (1, 1, 5.0, 85.50, '2025-01-15', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
-- Replace with your actual data from: SELECT * FROM transactions;
;


-- STEP 4: Export all price snapshots
-- ============================================================================
-- NOTE: Replace the data below with your actual price snapshots from the command:
-- SELECT * FROM price_snapshots;

INSERT INTO price_snapshots (id, ticker, price, snapshot_date, source, created_at)
VALUES
-- EXAMPLE: (1, 'VWRL.L', 85.50, '2026-04-03', 'Eodhd', CURRENT_TIMESTAMP)
-- Replace with your actual data from: SELECT * FROM price_snapshots;
;


-- STEP 5: Export all projection settings
-- ============================================================================
-- NOTE: Replace the data below with your actual projection settings from the command:
-- SELECT * FROM projection_settings;

INSERT INTO projection_settings (id, user_id, yearly_return_percent, monthly_buy_amount, annual_buy_increase_percent, projection_years, inflation_percent, cgt_percent, exit_tax_percent, exclude_pre_existing_from_tax, created_at, updated_at)
VALUES
-- EXAMPLE: (1, 1, 7.5, 500.00, 2.0, 30, 2.5, 20.0, 10.0, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
-- Replace with your actual data from: SELECT * FROM projection_settings;
;


-- ============================================================================
-- HOW TO USE THIS FILE
-- ============================================================================
--
-- 1. EXPORT YOUR LOCAL DATA:
--    Run these queries in your local database and copy the results:
--
--    SELECT 'INSERT INTO users (id, email, first_name, last_name, created_at, updated_at) VALUES' AS section;
--    SELECT id, email, first_name, last_name, created_at, updated_at FROM users;
--
--    SELECT 'INSERT INTO holdings (id, user_id, ticker, etf_name, quantity, average_cost, broker, price_source, created_at, updated_at) VALUES' AS section;
--    SELECT id, user_id, ticker, etf_name, quantity, average_cost, broker, price_source, created_at, updated_at FROM holdings;
--
--    SELECT 'INSERT INTO transactions (id, holding_id, quantity, purchase_price, purchase_date, created_at, updated_at) VALUES' AS section;
--    SELECT id, holding_id, quantity, purchase_price, purchase_date, created_at, updated_at FROM transactions;
--
--    SELECT 'INSERT INTO price_snapshots (id, ticker, price, snapshot_date, source, created_at) VALUES' AS section;
--    SELECT id, ticker, price, snapshot_date, source, created_at FROM price_snapshots;
--
--    SELECT 'INSERT INTO projection_settings (id, user_id, yearly_return_percent, monthly_buy_amount, annual_buy_increase_percent, projection_years, inflation_percent, cgt_percent, exit_tax_percent, exclude_pre_existing_from_tax, created_at, updated_at) VALUES' AS section;
--    SELECT id, user_id, yearly_return_percent, monthly_buy_amount, annual_buy_increase_percent, projection_years, inflation_percent, cgt_percent, exit_tax_percent, exclude_pre_existing_from_tax, created_at, updated_at FROM projection_settings;
--
-- 2. REPLACE THE PLACEHOLDER DATA:
--    Copy the actual rows from your local database and replace the example data in this file.
--    Each row should be formatted as a tuple with values separated by commas.
--
-- 3. IMPORT INTO RENDER.COM:
--    a) Connect to your Render PostgreSQL database using psql or DBeaver
--    b) Run this SQL file
--    c) Your data will be imported
--
-- 4. VERIFY THE IMPORT:
--    SELECT COUNT(*) FROM users;
--    SELECT COUNT(*) FROM holdings;
--    SELECT COUNT(*) FROM transactions;
--    SELECT COUNT(*) FROM price_snapshots;
--    SELECT COUNT(*) FROM projection_settings;
--
-- ============================================================================
-- ALTERNATIVE METHOD: Using pg_dump for complete backup
-- ============================================================================
--
-- For a complete database backup, use pg_dump from command line:
--
-- 1. CREATE A BACKUP (run in terminal):
--    pg_dump -U postgres -h localhost etf_tracker > backup.sql
--
-- 2. RESTORE TO RENDER:
--    psql -U postgres -h your-render-database-host.onrender.com -d etf_tracker < backup.sql
--
-- Note: You'll need to replace the database connection details with your Render credentials
--
-- ============================================================================

