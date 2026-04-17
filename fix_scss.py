path = "/Users/denisbahia/RiderProjects/Bob Esponja/ETFTracker.Web/src/app/pages/dashboard/dashboard.component.scss"

with open(path, "r") as f:
    content = f.read()

# Strip any corruption after our marker
marker = "/* \u2500\u2500 Taxes column button"
idx = content.find(marker)
if idx != -1:
    content = content[:idx]

styles = """
/* -- Taxes column button -- */
.taxes-cell { white-space: nowrap; }

.btn-taxes {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  color: var(--text-secondary);
  border-radius: 8px;
  padding: 0.3rem 0.75rem;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  font-family: inherit;
  transition: border-color 0.15s, background 0.15s;
}
.btn-taxes:hover {
  border-color: var(--accent);
  background: rgba(79,142,247,0.08);
  color: var(--accent);
}
.btn-taxes--pending {
  border-color: rgba(248,155,41,0.5);
  background: rgba(248,155,41,0.07);
  color: #f89b29;
}
.btn-taxes--pending:hover { background: rgba(248,155,41,0.14); }

.taxes-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #f89b29;
  flex-shrink: 0;
  animation: pulse-dot 2s infinite;
}
@keyframes pulse-dot {
  0%, 100% { opacity: 1; transform: scale(1); }
  50%       { opacity: 0.5; transform: scale(1.5); }
}

/* -- Tax Summary Section -- */
.tax-summary-section {
  background: var(--bg-surface);
  border: 1px solid var(--border);
  border-radius: 14px;
  padding: 1.8rem 2rem;
  margin-top: 1.5rem;
}
.tax-summary-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.tax-summary-header h2 {
  font-size: 1.15rem;
  font-weight: 800;
  color: var(--text-primary);
  margin: 0 0 0.3rem;
}
.tax-summary-sub { font-size: 0.83rem; color: var(--text-muted); margin: 0; }
.tax-summary-actions { display: flex; gap: 0.75rem; flex-wrap: wrap; }
.tax-summary-loading { color: var(--text-muted); font-size: 0.88rem; padding: 1rem 0; }

.btn-mark-all {
  background: rgba(20,217,144,0.1);
  color: var(--positive);
  border: 1px solid rgba(20,217,144,0.3);
  border-radius: 8px;
  padding: 0.45rem 1rem;
  font-size: 0.83rem;
  font-weight: 600;
  cursor: pointer;
  font-family: inherit;
  transition: background 0.15s;
}
.btn-mark-all:hover:not(:disabled) { background: rgba(20,217,144,0.18); }
.btn-mark-all:disabled { opacity: 0.5; cursor: wait; }
.btn-mark-all--all {
  background: rgba(79,142,247,0.1);
  color: var(--accent);
  border-color: rgba(79,142,247,0.3);
}
.btn-mark-all--all:hover:not(:disabled) { background: rgba(79,142,247,0.18); }

.tax-overview-cards {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  margin-bottom: 1.8rem;
}
.tax-ov-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 0.8rem 1.3rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 150px;
}
.tax-ov-card--pending { border-color: rgba(248,155,41,0.4); background: rgba(248,155,41,0.05); }
.tax-ov-card--year    { border-color: rgba(240,82,82,0.35);  background: rgba(240,82,82,0.04); }
.tax-ov-card--paid    { border-color: rgba(20,217,144,0.3);  background: rgba(20,217,144,0.04); }

.tax-ov-label { font-size: 0.72rem; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
.tax-ov-value { font-size: 1.05rem; font-weight: 700; color: var(--text-primary); }
.tax-ov-value--green  { color: var(--positive); }
.tax-ov-value--accent { color: var(--accent); font-size: 0.9rem; }

.tax-empty {
  color: var(--text-secondary);
  font-size: 0.88rem;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 1.2rem 1.5rem;
}

.tax-holding-group { margin-bottom: 1.4rem; }
.tax-holding-group:last-child { margin-bottom: 0; }

.tax-holding-header {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 0.8rem;
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  border-bottom: none;
  border-radius: 10px 10px 0 0;
  font-size: 0.88rem;
}
.tax-holding-header strong { color: var(--text-primary); font-size: 0.95rem; }
.tax-holding-name { color: var(--text-muted); font-size: 0.82rem; }
.tax-holding-pending {
  margin-left: auto;
  font-size: 0.78rem;
  font-weight: 700;
  color: #f89b29;
  background: rgba(248,155,41,0.1);
  border: 1px solid rgba(248,155,41,0.3);
  border-radius: 6px;
  padding: 0.15rem 0.55rem;
}

.tax-events-table-wrap {
  overflow-x: auto;
  border: 1px solid var(--border);
  border-radius: 0 0 10px 10px;
}
.tax-events-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.83rem;
}
.tax-events-table th {
  padding: 0.5rem 0.9rem;
  background: var(--bg-card);
  color: var(--text-secondary);
  font-weight: 600;
  text-align: left;
  white-space: nowrap;
}
.tax-events-table td {
  padding: 0.55rem 0.9rem;
  color: var(--text-primary);
  border-top: 1px solid var(--border);
  white-space: nowrap;
}
.tax-events-table tr.tax-row-pending td { background: rgba(248,155,41,0.025); }
.tax-events-table tr.tax-row-paid td    { opacity: 0.65; }

.te-type-badge {
  display: inline-block;
  border-radius: 5px;
  padding: 0.15rem 0.5rem;
  font-size: 0.74rem;
  font-weight: 600;
}
.te-type-badge--disposal {
  background: rgba(79,142,247,0.12);
  color: var(--accent);
  border: 1px solid rgba(79,142,247,0.25);
}
.te-type-badge--sell {
  background: rgba(124,92,252,0.12);
  color: var(--accent-purple);
  border: 1px solid rgba(124,92,252,0.25);
}
.gain-pos { color: var(--positive); }
.gain-neg { color: var(--negative); }
.te-tax-amt { font-weight: 700; color: var(--negative); }

.te-status {
  display: inline-block;
  border-radius: 5px;
  padding: 0.15rem 0.5rem;
  font-size: 0.73rem;
  font-weight: 700;
}
.te-status--pending {
  background: rgba(248,155,41,0.12);
  color: #f89b29;
  border: 1px solid rgba(248,155,41,0.3);
}
.te-status--paid {
  background: rgba(20,217,144,0.1);
  color: var(--positive);
  border: 1px solid rgba(20,217,144,0.25);
}

.btn-te-paid {
  background: rgba(20,217,144,0.1);
  color: var(--positive);
  border: 1px solid rgba(20,217,144,0.3);
  border-radius: 5px;
  padding: 0.2rem 0.6rem;
  font-size: 0.74rem;
  font-weight: 600;
  cursor: pointer;
  font-family: inherit;
  transition: background 0.15s;
}
.btn-te-paid:hover { background: rgba(20,217,144,0.2); }

.te-paid-on { font-size: 0.74rem; color: var(--text-muted); }
"""

with open(path, "w") as f:
    f.write(content + styles)

print("Done - scss updated successfully")

