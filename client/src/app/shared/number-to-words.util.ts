// Indian/BD-style number-to-words (Lakh/Crore grouping) — used for invoice / receipt printouts.

const ONES = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine'];
const TEENS = ['Ten', 'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

function twoDigit(n: number): string {
  if (n < 10) return ONES[n];
  if (n < 20) return TEENS[n - 10];
  const t = Math.floor(n / 10), o = n % 10;
  return TENS[t] + (o ? ' ' + ONES[o] : '');
}

function threeDigit(n: number): string {
  const h = Math.floor(n / 100), rest = n % 100;
  const hPart = h ? ONES[h] + ' Hundred' : '';
  const rPart = rest ? twoDigit(rest) : '';
  return hPart && rPart ? hPart + ' ' + rPart : (hPart || rPart);
}

/**
 * Spell out a money amount in BD/IN grouping. The fractional-unit word defaults to "Paisa"
 * (BDT) but can be overridden per currency (e.g. "Cents" for USD) so receipts/invoices in a
 * foreign currency read correctly. The major-unit word ("Taka" / "US Dollars" / …) is appended
 * by the caller.
 */
export function numberToWords(amount: number, minorUnit: string = 'Paisa'): string {
  if (amount == null || isNaN(amount)) return '';
  const negative = amount < 0;
  amount = Math.abs(amount);
  const whole = Math.floor(amount);
  const paisa = Math.round((amount - whole) * 100);

  if (whole === 0 && paisa === 0) return 'Zero';

  // BD/IN grouping: Crore (10M), Lakh (100K), Thousand, Hundreds
  const crore = Math.floor(whole / 10_000_000);
  const lakh = Math.floor((whole % 10_000_000) / 100_000);
  const thousand = Math.floor((whole % 100_000) / 1_000);
  const hundreds = whole % 1_000;

  const parts: string[] = [];
  if (crore) parts.push(twoDigit(crore) + ' Crore');
  if (lakh) parts.push(twoDigit(lakh) + ' Lakh');
  if (thousand) parts.push(twoDigit(thousand) + ' Thousand');
  if (hundreds) parts.push(threeDigit(hundreds));

  let text = parts.join(' ');
  if (paisa) text += ' and ' + twoDigit(paisa) + ' ' + minorUnit;
  return (negative ? 'Negative ' : '') + text;
}
