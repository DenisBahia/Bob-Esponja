-- Migration: Update purchase_date column type to DATE
-- This migration converts the purchase_date column from TIMESTAMP to DATE

-- Drop the default value constraint if it exists
ALTER TABLE transactions 
  ALTER COLUMN purchase_date DROP DEFAULT;

-- Cast the column to DATE type
ALTER TABLE transactions 
  ALTER COLUMN purchase_date TYPE DATE USING purchase_date::DATE;

-- Verify the change
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name='transactions' AND column_name='purchase_date';

