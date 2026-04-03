#!/usr/bin/env python3
"""
Script to import historical price data from Yahoo Finance into the price_snapshots table.
"""

import yfinance as yf
import psycopg2
import pandas as pd
from datetime import datetime
from typing import List, Dict

# Database configuration
DB_CONFIG = {
    'host': 'localhost',
    'port': 5432,
    'database': 'etf_tracker',
    'user': 'denisbahia',
    'password': 'postgres'
}

# Ticker and date range
TICKERS = ['IXUA.DE', 'E500.DE', 'FWIA.DE']
START_DATE = '2026-01-01'
END_DATE = None  # None means up to today

def fetch_historical_prices(ticker: str, start_date: str, end_date: str = None) -> List[Dict]:
    """
    Fetch historical price data from Yahoo Finance.
    Returns a list of dictionaries with 'date', 'close' keys.
    """
    print(f"Fetching historical prices for {ticker} from {start_date}...")
    
    try:
        data = yf.download(ticker, start=start_date, end=end_date, progress=False)
        
        if data.empty:
            print(f"No data found for {ticker}")
            return []
        
        prices = []
        for date, row in data.iterrows():
            try:
                close_price = float(row['Close'])
                prices.append({
                    'date': date.to_pydatetime(),
                    'close': close_price
                })
            except (ValueError, TypeError):
                continue
        
        print(f"Fetched {len(prices)} price records")
        return prices
    except Exception as e:
        print(f"Error fetching data: {e}")
        import traceback
        traceback.print_exc()
        return []

def import_to_database(ticker: str, prices: List[Dict]):
    """
    Import price data into the price_snapshots table.
    """
    if not prices:
        print("No prices to import")
        return
    
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()
        
        inserted = 0
        skipped = 0
        
        for price_data in prices:
            snapshot_date = price_data['date']
            price = price_data['close']
            source = 'Yahoo Finance'
            created_at = datetime.utcnow()
            
            try:
                # Check if this price already exists
                cursor.execute(
                    "SELECT id FROM price_snapshots WHERE ticker = %s AND snapshot_date = %s",
                    (ticker, snapshot_date)
                )
                
                if cursor.fetchone():
                    skipped += 1
                    continue
                
                # Insert the price
                cursor.execute(
                    """
                    INSERT INTO price_snapshots (ticker, price, snapshot_date, source, created_at)
                    VALUES (%s, %s, %s, %s, %s)
                    """,
                    (ticker, price, snapshot_date, source, created_at)
                )
                inserted += 1
            except Exception as e:
                print(f"Error inserting price for {snapshot_date}: {e}")
        
        conn.commit()
        cursor.close()
        conn.close()
        
        print(f"Import complete: {inserted} inserted, {skipped} already existed")
        
    except Exception as e:
        print(f"Database error: {e}")

def main():
    """Main function to orchestrate the import process."""
    print(f"=== Price Data Importer ===")
    print(f"Tickers: {', '.join(TICKERS)}")
    print(f"Start Date: {START_DATE}")
    print()
    
    for ticker in TICKERS:
        print(f"--- Processing {ticker} ---")
        # Fetch prices
        prices = fetch_historical_prices(ticker, START_DATE, END_DATE)
        
        if prices:
            # Import to database
            import_to_database(ticker, prices)
        else:
            print(f"Failed to fetch prices for {ticker}")
        print()

if __name__ == '__main__':
    main()



