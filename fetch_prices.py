#!/usr/bin/env python3
"""
Fetches historical close prices from Yahoo Finance Chart API for 3 ETFs,
prints a confirmation table, and generates SQL INSERT statements for price_snapshots.
"""

import argparse
import json
import subprocess
import sys
from datetime import datetime, timezone

TICKERS = ["IXUA.DE", "FWIA.DE", "E500.DE"]
START_DATE = datetime(2026, 1, 1, tzinfo=timezone.utc)
SOURCE = "Yahoo"

INSERT_TICKER_MAP = {
    "FWIA.DE": "FWIA.XETRA",
    "E500.DE": "E500.XETRA",
}


def fetch_yahoo(ticker: str) -> list[dict]:
    url = (
        f"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}"
        f"?interval=1d&range=120d"
    )
    result = subprocess.run(
        ["curl", "-s", "-H", "User-Agent: Mozilla/5.0", url],
        capture_output=True, text=True, timeout=20
    )
    if result.returncode != 0:
        raise RuntimeError(f"curl failed: {result.stderr}")

    data = json.loads(result.stdout)
    chart_result = data["chart"]["result"][0]
    timestamps = chart_result["timestamp"]
    closes = chart_result["indicators"]["quote"][0]["close"]

    rows = []
    for ts, price in zip(timestamps, closes):
        if price is None:
            continue
        date = datetime.fromtimestamp(ts, tz=timezone.utc).date()
        if datetime(date.year, date.month, date.day, tzinfo=timezone.utc) < START_DATE:
            continue
        rows.append({"ticker": ticker, "date": date, "price": round(price, 4)})

    return rows


def print_table(all_rows: list[dict]):
    print(f"\n{'Ticker':<12} {'Date':<14} {'Close Price':>12}")
    print("-" * 40)
    for r in all_rows:
        print(f"{r['ticker']:<12} {str(r['date']):<14} {r['price']:>12.4f}")
    print(f"\nTotal rows: {len(all_rows)}\n")


def map_ticker_for_insert(ticker: str) -> str:
    return INSERT_TICKER_MAP.get(ticker, ticker)


def generate_sql(all_rows: list[dict]) -> str:
    now = datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S")
    lines = [
        "BEGIN;",
        "DELETE FROM price_snapshots;",
    ]
    for r in all_rows:
        insert_ticker = map_ticker_for_insert(r["ticker"])
        lines.append(
            f"INSERT INTO price_snapshots (ticker, price, snapshot_date, source, created_at, updated_at) "
            f"VALUES ('{insert_ticker}', {r['price']}, '{r['date']}', '{SOURCE}', '{now}', '{now}') "
            f"ON CONFLICT (ticker, snapshot_date) DO NOTHING;"
        )
    lines.append("COMMIT;")
    return "\n".join(lines)


def run_on_db(db_url: str, sql_file: str):
    """Execute the generated SQL file against the given PostgreSQL URL via psql."""
    print(f"\nConnecting to database and running inserts...")
    result = subprocess.run(
        ["psql", db_url, "-f", sql_file, "-v", "ON_ERROR_STOP=1"],
        capture_output=True, text=True
    )
    if result.returncode != 0:
        print(f"❌ psql error:\n{result.stderr}")
        sys.exit(1)

    inserted = result.stdout.count("INSERT 0 1")
    skipped  = result.stdout.count("INSERT 0 0")
    print(f"✅ Done — {inserted} inserted, {skipped} already existed (skipped).")


def main():
    parser = argparse.ArgumentParser(description="Fetch ETF prices and insert into price_snapshots.")
    parser.add_argument(
        "--db-url",
        metavar="URL",
        help="PostgreSQL connection URL (e.g. postgresql://user:pass@host:5432/db). "
             "When provided the SQL is executed immediately against that database."
    )
    args = parser.parse_args()

    all_rows = []
    for ticker in TICKERS:
        print(f"Fetching {ticker}...", end=" ", flush=True)
        try:
            rows = fetch_yahoo(ticker)
            print(f"{len(rows)} rows from 2026-01-01 onwards")
            all_rows.extend(rows)
        except Exception as e:
            print(f"ERROR: {e}")

    if not all_rows:
        print("No data fetched. Exiting.")
        return

    # Sort by ticker then date for readability
    all_rows.sort(key=lambda r: (r["ticker"], r["date"]))

    print_table(all_rows)

    sql = generate_sql(all_rows)

    output_file = "insert_prices.sql"
    with open(output_file, "w") as f:
        f.write("-- Auto-generated price inserts\n")
        f.write("-- Full refresh mode: deletes all rows from price_snapshots before insert\n")
        f.write(f"-- Generated: {datetime.utcnow().strftime('%Y-%m-%d %H:%M:%S')} UTC\n")
        f.write(f"-- Tickers: {', '.join(TICKERS)}\n")
        f.write(f"-- Rows: {len(all_rows)}\n\n")
        f.write(sql)
        f.write("\n")

    print(f"SQL written to: {output_file}")

    if args.db_url:
        run_on_db(args.db_url, output_file)
    else:
        print("\nTip: pass --db-url 'postgresql://user:pass@host:5432/db' to run directly against a database.")


if __name__ == "__main__":
    main()

