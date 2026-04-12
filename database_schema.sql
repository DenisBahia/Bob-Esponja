-- Investment Portfolio Tracking Application Database Schema
-- PostgreSQL

-- Create ENUM types
CREATE TYPE broker_type AS ENUM ('Interactive Brokers', 'Trading 212', 'Wise', 'Other');

-- Users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Holdings table (one record per ticker per user)
CREATE TABLE holdings (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    ticker VARCHAR(20) NOT NULL,
    etf_name VARCHAR(255),
    quantity DECIMAL(18, 8) NOT NULL DEFAULT 0,
    average_cost DECIMAL(18, 8) NOT NULL DEFAULT 0,
    broker VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, ticker)
);

-- Transactions table (buy history)
CREATE TABLE transactions (
    id SERIAL PRIMARY KEY,
    holding_id INTEGER NOT NULL REFERENCES holdings(id) ON DELETE CASCADE,
    quantity DECIMAL(18, 8) NOT NULL,
    purchase_price DECIMAL(18, 8) NOT NULL,
    purchase_date DATE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Price Snapshots table (daily snapshots for historical tracking)
CREATE TABLE price_snapshots (
    id SERIAL PRIMARY KEY,
    ticker VARCHAR(20) NOT NULL,
    price DECIMAL(18, 8) NOT NULL,
    snapshot_date DATE NOT NULL,
    source VARCHAR(50), -- 'Eodhd' or 'Yahoo'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(ticker, snapshot_date)
);

-- Create indexes for performance
CREATE INDEX idx_holdings_user_id ON holdings(user_id);
CREATE INDEX idx_holdings_ticker ON holdings(ticker);
CREATE INDEX idx_transactions_holding_id ON transactions(holding_id);
CREATE INDEX idx_price_snapshots_ticker ON price_snapshots(ticker);
CREATE INDEX idx_price_snapshots_date ON price_snapshots(snapshot_date);
CREATE INDEX idx_transactions_purchase_date ON transactions(purchase_date);

-- Create view for current prices (latest snapshot)
CREATE VIEW latest_prices AS
SELECT DISTINCT ON (ticker) 
    ticker, 
    price, 
    snapshot_date, 
    source
FROM price_snapshots
ORDER BY ticker, snapshot_date DESC;

-- Trigger to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_holdings_updated_at BEFORE UPDATE ON holdings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at BEFORE UPDATE ON transactions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

