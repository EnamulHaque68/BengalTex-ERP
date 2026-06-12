/**
 * Runtime UI translations for operator-facing (shop-floor) screens.
 * One flat dictionary keyed by translation key, each entry carrying both languages —
 * easier to keep en/bn in sync than two parallel files. Office screens stay English;
 * add keys here only as screens get translated.
 */
export type Lang = 'en' | 'bn';

export const TRANSLATIONS: Record<string, { en: string; bn: string }> = {
  // ── Common ────────────────────────────────────────────────────────────────
  'common.date':        { en: 'Date',     bn: 'তারিখ' },
  'common.time':        { en: 'Time',     bn: 'সময়' },
  'common.status':      { en: 'Status',   bn: 'অবস্থা' },
  'common.notes':       { en: 'Notes',    bn: 'নোট' },
  'common.optional':    { en: '(optional)', bn: '(ঐচ্ছিক)' },

  // ── Attendance status values (enum text from the API) ────────────────────
  'status.Present':     { en: 'Present',  bn: 'উপস্থিত' },
  'status.Late':        { en: 'Late',     bn: 'দেরি' },
  'status.Absent':      { en: 'Absent',   bn: 'অনুপস্থিত' },
  'status.OnLeave':     { en: 'On Leave', bn: 'ছুটিতে' },
  'status.HalfDay':     { en: 'Half Day', bn: 'অর্ধদিবস' },

  // ── Self check-in screen ──────────────────────────────────────────────────
  'checkin.title':      { en: 'Self Check-In', bn: 'সেলফ চেক-ইন' },
  'checkin.subtitle':   {
    en: "Mark yourself Present for today — GPS location validates you're inside the factory geo-fence",
    bn: 'আজকের জন্য নিজেকে উপস্থিত হিসেবে চিহ্নিত করুন — GPS লোকেশন যাচাই করবে আপনি ফ্যাক্টরির সীমানার ভিতরে আছেন কিনা'
  },
  'checkin.location':   { en: 'Location', bn: 'লোকেশন' },
  'checkin.gps.idle':        { en: 'Waiting...', bn: 'অপেক্ষা চলছে...' },
  'checkin.gps.requesting':  { en: 'Requesting your location...', bn: 'আপনার লোকেশন নেওয়া হচ্ছে...' },
  'checkin.gps.granted':     { en: 'Location captured', bn: 'লোকেশন পাওয়া গেছে' },
  'checkin.gps.denied':      { en: 'Permission denied', bn: 'অনুমতি দেওয়া হয়নি' },
  'checkin.gps.unavailable': { en: 'GPS not available', bn: 'GPS পাওয়া যাচ্ছে না' },
  'checkin.gps.error':       { en: 'Location error', bn: 'লোকেশন ত্রুটি' },
  'checkin.latitude':   { en: 'Latitude',  bn: 'অক্ষাংশ' },
  'checkin.longitude':  { en: 'Longitude', bn: 'দ্রাঘিমাংশ' },
  'checkin.accuracy':   { en: 'Accuracy',  bn: 'নির্ভুলতা' },
  'checkin.retryGps':   { en: 'Retry GPS', bn: 'আবার GPS চেষ্টা করুন' },
  'checkin.checkIn':    { en: 'Check In',  bn: 'চেক ইন' },
  'checkin.notesPlaceholder': {
    en: 'Anything to flag — late arrival reason, work from home, etc.',
    bn: 'কিছু জানানোর থাকলে লিখুন — দেরিতে আসার কারণ ইত্যাদি'
  },
  'checkin.noGpsHint': {
    en: "You can check in without GPS — it just won't be validated against the geo-fence.",
    bn: 'GPS ছাড়াও চেক ইন করতে পারবেন — শুধু জিও-ফেন্স যাচাই হবে না।'
  },
  'checkin.checkingIn': { en: 'Checking in...', bn: 'চেক ইন হচ্ছে...' },
  'checkin.checkInNow': { en: 'Check In Now',   bn: 'এখনই চেক ইন করুন' },
  'checkin.checkedIn':  { en: 'Checked In',     bn: 'চেক ইন সম্পন্ন' },
  'checkin.geoFence':   { en: 'Geo-fence',      bn: 'জিও-ফেন্স' },
  'checkin.insideFactory': { en: 'Inside factory', bn: 'ফ্যাক্টরির ভিতরে' },
  'checkin.outsideFence':  { en: 'Outside fence',  bn: 'সীমানার বাইরে' },
  'checkin.away':          { en: 'away',           bn: 'দূরে' },
  'checkin.flagged':       { en: 'flagged for review', bn: 'পর্যালোচনার জন্য চিহ্নিত' },
  'checkin.fenceNotConfigured': {
    en: 'Not configured — location recorded but not validated',
    bn: 'কনফিগার করা নেই — লোকেশন রেকর্ড হয়েছে কিন্তু যাচাই হয়নি'
  },
  'checkin.recordedHint': {
    en: 'Your check-in is recorded. Have a productive day!',
    bn: 'আপনার চেক-ইন রেকর্ড হয়েছে। দিনটি ভালো কাটুক!'
  }
};
