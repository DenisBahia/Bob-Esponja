DATABASE EXPORT & IMPORT GUIDE FOR RENDER.COM
==============================================

📊 Overview
-----------
This guide shows you how to export all your local database data and import it into your Render.com PostgreSQL database.

✅ What Will Be Exported
------------------------
✓ Users
✓ Holdings (ETF positions)
✓ Transactions (buy history)
✓ Price Snapshots (historical prices)
✓ Projection Settings (your investment projections)


METHOD 1: Using pg_dump (Recommended - Easiest)
================================================

Step 1: Export Your Local Database
-----------------------------------
Open a terminal and run:

    pg_dump -U postgres -h localhost -d etf_tracker -f backup.sql

This creates a complete backup file "backup.sql"

Parameters:
  -U postgres        = your PostgreSQL username
  -h localhost       = your local database host
  -d etf_tracker     = your local database name
  -f backup.sql      = output filename


Step 2: Connect to Render Database
-----------------------------------
Get your Render database credentials:
1. Go to https://dashboard.render.com
2. Click your PostgreSQL database
3. Copy the External Database URL

It looks like:
  postgresql://username:password@hostname.onrender.com:5432/dbname


Step 3: Import the Backup
--------------------------
Option A - Using psql (command line):

    psql postgresql://username:password@hostname.onrender.com:5432/dbname < backup.sql

Option B - Using DBeaver (GUI):
    1. Open DBeaver
    2. Connect to your Render database
    3. Go to Tools → Execute Script
    4. Select backup.sql
    5. Click Execute


Step 4: Verify the Import
--------------------------
Connect to your Render database and run:

    SELECT COUNT(*) as users_count FROM users;
    SELECT COUNT(*) as holdings_count FROM holdings;
    SELECT COUNT(*) as transactions_count FROM transactions;
    SELECT COUNT(*) as price_snapshots_count FROM price_snapshots;
    SELECT COUNT(*) as projection_settings_count FROM projection_settings;

You should see your data counts!


METHOD 2: Using the Manual SQL Export File
===========================================

This method gives you more control and is useful if pg_dump isn't available.

Step 1: Extract Your Data Locally
---------------------------------
Connect to your local database and run these queries one by one:

    -- Get users
    SELECT id, email, first_name, last_name, created_at, updated_at FROM users;

    -- Get holdings
    SELECT id, user_id, ticker, etf_name, quantity, average_cost, broker, price_source, created_at, updated_at FROM holdings;

    -- Get transactions
    SELECT id, holding_id, quantity, purchase_price, purchase_date, created_at, updated_at FROM transactions;

    -- Get price snapshots
    SELECT id, ticker, price, snapshot_date, source, created_at FROM price_snapshots;

    -- Get projection settings
    SELECT id, user_id, yearly_return_percent, monthly_buy_amount, annual_buy_increase_percent, projection_years, inflation_percent, cgt_percent, exit_tax_percent, exclude_pre_existing_from_tax, created_at, updated_at FROM projection_settings;


Step 2: Format the Results
--------------------------
Copy each result set and format it as SQL INSERT statements.

Example:
  If your users table has:
    id=1, email='user@example.com', first_name='John', last_name='Doe'
  
  Format it as:
    INSERT INTO users (id, email, first_name, last_name, created_at, updated_at) 
    VALUES (1, 'user@example.com', 'John', 'Doe', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);


Step 3: Edit EXPORT_DATA_FOR_RENDER.sql
---------------------------------------
1. Open the file: EXPORT_DATA_FOR_RENDER.sql
2. Replace each STEP's placeholder data with your actual data
3. Save the file


Step 4: Import into Render
--------------------------
Connect to your Render database and:

    Option A (Command line):
    psql postgresql://username:password@hostname.onrender.com:5432/dbname < EXPORT_DATA_FOR_RENDER.sql

    Option B (DBeaver):
    Tools → Execute Script → Select EXPORT_DATA_FOR_RENDER.sql → Execute


USING DBeaver GUI (No Command Line)
===================================

Step 1: Add Connection to Local Database
----------------------------------------
1. Open DBeaver
2. Database → New Database Connection
3. Select PostgreSQL
4. Host: localhost, Port: 5432, Username: postgres
5. Database: etf_tracker
6. Click Test Connection → Finish


Step 2: Export Data
-------------------
1. Right-click "etf_tracker" → SQL Editor → Open SQL Script
2. Copy and run these queries:

    SELECT * FROM users;
    SELECT * FROM holdings;
    SELECT * FROM transactions;
    SELECT * FROM price_snapshots;
    SELECT * FROM projection_settings;

3. Export each result as INSERT statements (Right-click table → Generate SQL → Insert)


Step 3: Add Connection to Render Database
------------------------------------------
1. Database → New Database Connection
2. Select PostgreSQL
3. Host: your-db-hostname.onrender.com
4. Port: 5432
5. Database: your-db-name
6. Username & Password from your Render dashboard
7. Click Test Connection → Finish


Step 4: Execute Import
---------------------
1. Open EXPORT_DATA_FOR_RENDER.sql in DBeaver
2. Tools → Execute Script
3. Select your Render database connection
4. Click Execute


⚠️ Important Notes
==================

1. SEQUENCES / ID NUMBERS
   - If you have existing data in Render, you may need to reset sequences
   - After import, run: SELECT setval('users_id_seq', (SELECT MAX(id) FROM users));
   - Do this for each table: holdings, transactions, price_snapshots, projection_settings

2. CONSTRAINTS
   - The database will enforce foreign key relationships
   - Make sure user_ids exist before importing holdings
   - Make sure holding_ids exist before importing transactions

3. TIMEZONE
   - PostgreSQL stores timestamps in UTC
   - The import will preserve your timestamps

4. BACKUPS
   - Always keep a backup before importing
   - Render provides automatic backups on paid plans

5. DATA VALIDATION
   - After import, verify counts and sample records
   - Check that all your holdings are there
   - Verify transactions are linked correctly


TROUBLESHOOTING
===============

Error: "connection refused"
  ✓ Make sure your Render database is created and running
  ✓ Check your connection credentials (copy carefully from Render dashboard)
  ✓ Make sure your IP is whitelisted (Render allows all IPs for PostgreSQL)

Error: "relation does not exist"
  ✓ Run the migration first: npm run build && dotnet ef database update
  ✓ This creates the schema on Render

Error: "duplicate key value violates unique constraint"
  ✓ You may have existing data in Render
  ✓ Clear it first: DELETE FROM holdings; DELETE FROM transactions; DELETE FROM price_snapshots; DELETE FROM users;
  ✓ Then import

Error: "foreign key violation"
  ✓ Import in order: users → holdings → transactions → price_snapshots → projection_settings
  ✓ Make sure all referenced IDs exist

Need help with PostgreSQL connection string?
  ✓ Format: postgresql://username:password@hostname:5432/database_name
  ✓ Copy from your Render PostgreSQL dashboard


NEXT STEPS
==========

1. Export your data using Method 1 (pg_dump) - it's the easiest
2. Connect to your Render database
3. Import the backup.sql file
4. Verify the data with SELECT COUNT(*) queries
5. Test your application - it should work with your imported data!

Questions? Check:
  ✓ RENDER_DEPLOYMENT_STEPS.md for Render setup details
  ✓ database_schema.sql for database structure
  ✓ Render Dashboard documentation

Good luck! 🚀

