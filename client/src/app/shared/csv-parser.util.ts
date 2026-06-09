/**
 * Tiny RFC4180-friendly CSV parser. Handles:
 *  - quoted fields with embedded commas: "1,234.56"
 *  - escaped quotes: "He said ""hi"""
 *  - both LF and CRLF line endings
 *  - trailing newline + empty rows (skipped)
 * Not a full RFC implementation — but covers every BD bank export we've seen.
 *
 * Returns rows as string arrays; caller maps columns by header.
 */
export function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let field = '';
  let inQuotes = false;
  let i = 0;
  while (i < text.length) {
    const c = text[i];
    if (inQuotes) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i += 2; continue; }
        inQuotes = false; i++; continue;
      }
      field += c; i++; continue;
    } else {
      if (c === '"') { inQuotes = true; i++; continue; }
      if (c === ',') { row.push(field); field = ''; i++; continue; }
      if (c === '\r') { i++; continue; }
      if (c === '\n') {
        row.push(field);
        if (row.length > 1 || (row.length === 1 && row[0] !== '')) rows.push(row);
        row = []; field = ''; i++; continue;
      }
      field += c; i++;
    }
  }
  if (field !== '' || row.length > 0) {
    row.push(field);
    if (row.length > 1 || (row.length === 1 && row[0] !== '')) rows.push(row);
  }
  return rows;
}

/** Trim header names and lowercase for fuzzy matching. */
export function normHeader(s: string): string {
  return s.trim().toLowerCase().replace(/[\s_-]/g, '');
}

/**
 * Parse a CSV string into bank-statement line shapes. Auto-detects header columns by
 * fuzzy matching common BD bank statement formats:
 *   Date columns: "date", "transactiondate", "txndate", "valuedate"
 *   Description: "description", "narration", "particulars", "details", "remarks"
 *   Reference:   "reference", "refno", "chequeno", "trxid", "txnref"
 *   Amount (signed): "amount", "signedamount"
 *   OR Debit / Credit pair: "debit"+"credit", "withdrawal"+"deposit"
 * Returns parsed lines + warnings array (errors per-row don't abort the import).
 */
export interface CsvParseResult {
  lines: { transactionDate: string; description: string; referenceNumber: string | null; amount: number }[];
  warnings: string[];
  headerError: string | null;
}

const DATE_KEYS = ['date', 'transactiondate', 'txndate', 'valuedate', 'postingdate'];
const DESC_KEYS = ['description', 'narration', 'particulars', 'details', 'remarks'];
const REF_KEYS  = ['reference', 'refno', 'chequeno', 'trxid', 'txnref', 'referencenumber'];
const AMT_KEYS  = ['amount', 'signedamount', 'value'];
const DR_KEYS   = ['debit', 'withdrawal', 'dr'];
const CR_KEYS   = ['credit', 'deposit', 'cr'];

export function parseBankCsv(text: string): CsvParseResult {
  const rows = parseCsv(text);
  if (rows.length < 2) {
    return { lines: [], warnings: [], headerError: 'CSV needs at least a header row + one data row.' };
  }

  const headers = rows[0].map(normHeader);
  const findCol = (keys: string[]) => headers.findIndex(h => keys.includes(h));

  const dateIdx = findCol(DATE_KEYS);
  const descIdx = findCol(DESC_KEYS);
  const refIdx  = findCol(REF_KEYS);
  const amtIdx  = findCol(AMT_KEYS);
  const drIdx   = findCol(DR_KEYS);
  const crIdx   = findCol(CR_KEYS);

  if (dateIdx < 0) return { lines: [], warnings: [], headerError: 'Could not find a Date column. Expected one of: Date / Transaction Date / Value Date.' };
  if (descIdx < 0) return { lines: [], warnings: [], headerError: 'Could not find a Description column. Expected one of: Description / Narration / Particulars / Details.' };
  if (amtIdx < 0 && (drIdx < 0 || crIdx < 0)) {
    return { lines: [], warnings: [], headerError: 'Could not find Amount column. Expected either a single Amount column OR both Debit + Credit columns.' };
  }

  const lines: CsvParseResult['lines'] = [];
  const warnings: string[] = [];
  for (let r = 1; r < rows.length; r++) {
    const row = rows[r];
    if (row.every(c => c.trim() === '')) continue;          // skip blank
    const rawDate = (row[dateIdx] ?? '').trim();
    const desc = (row[descIdx] ?? '').trim();
    const ref = refIdx >= 0 ? (row[refIdx] ?? '').trim() : '';
    if (!rawDate || !desc) {
      warnings.push(`Row ${r + 1}: missing date or description, skipped.`);
      continue;
    }
    const parsedDate = parseFlexibleDate(rawDate);
    if (!parsedDate) {
      warnings.push(`Row ${r + 1}: unrecognised date "${rawDate}", skipped.`);
      continue;
    }
    let amount = 0;
    if (amtIdx >= 0) {
      amount = parseAmount(row[amtIdx]);
    } else {
      const dr = parseAmount(row[drIdx]);
      const cr = parseAmount(row[crIdx]);
      amount = cr - dr;     // credit (deposit) = inflow +, debit (withdrawal) = outflow −
    }
    if (amount === 0 || isNaN(amount)) {
      warnings.push(`Row ${r + 1}: amount is zero or invalid, skipped.`);
      continue;
    }
    lines.push({ transactionDate: parsedDate, description: desc, referenceNumber: ref || null, amount });
  }

  return { lines, warnings, headerError: null };
}

function parseAmount(s: string | undefined): number {
  if (!s) return 0;
  const cleaned = s.replace(/,/g, '').replace(/[৳$]/g, '').trim();
  if (cleaned === '' || cleaned === '-') return 0;
  const n = parseFloat(cleaned);
  return isNaN(n) ? 0 : n;
}

/**
 * Accepts common BD bank export date formats and returns ISO YYYY-MM-DD:
 *   2026-06-04          (ISO)
 *   04/06/2026          (DD/MM/YYYY — BD default)
 *   04-06-2026          (DD-MM-YYYY)
 *   04-Jun-2026 / 04 Jun 2026
 *   06/04/2026          (MM/DD/YYYY — heuristic: if first part > 12, treat as DD)
 */
function parseFlexibleDate(s: string): string | null {
  const t = s.trim();
  if (!t) return null;
  // ISO
  if (/^\d{4}-\d{2}-\d{2}$/.test(t)) return t;
  // dd-mmm-yyyy / dd mmm yyyy
  const monthNames: Record<string, string> = {
    jan: '01', feb: '02', mar: '03', apr: '04', may: '05', jun: '06',
    jul: '07', aug: '08', sep: '09', oct: '10', nov: '11', dec: '12'
  };
  const namedMatch = t.match(/^(\d{1,2})[\s-]+([A-Za-z]{3,9})[\s-]+(\d{2,4})$/);
  if (namedMatch) {
    const d = namedMatch[1].padStart(2, '0');
    const mKey = namedMatch[2].slice(0, 3).toLowerCase();
    const mm = monthNames[mKey];
    let yyyy = namedMatch[3];
    if (yyyy.length === 2) yyyy = (parseInt(yyyy) < 50 ? '20' : '19') + yyyy;
    if (mm) return `${yyyy}-${mm}-${d}`;
  }
  // dd/mm/yyyy or dd-mm-yyyy
  const slashMatch = t.match(/^(\d{1,2})[\/-](\d{1,2})[\/-](\d{2,4})$/);
  if (slashMatch) {
    let a = parseInt(slashMatch[1]);
    let b = parseInt(slashMatch[2]);
    let yyyy = slashMatch[3];
    if (yyyy.length === 2) yyyy = (parseInt(yyyy) < 50 ? '20' : '19') + yyyy;
    // Heuristic: a > 12 → a is day. b > 12 → b is day (US-style mm/dd).
    let day: number, month: number;
    if (a > 12) { day = a; month = b; }
    else if (b > 12) { day = b; month = a; }
    else { day = a; month = b; }   // default to DD/MM for BD
    return `${yyyy}-${month.toString().padStart(2, '0')}-${day.toString().padStart(2, '0')}`;
  }
  return null;
}
