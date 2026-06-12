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
  },

  // ── Job card statuses (enum text from the API) ────────────────────────────
  'jcstatus.Open':        { en: 'Open',        bn: 'খোলা' },
  'jcstatus.InProgress':  { en: 'In Progress', bn: 'চলছে' },
  'jcstatus.OnHold':      { en: 'On Hold',     bn: 'বিরতিতে' },
  'jcstatus.Completed':   { en: 'Completed',   bn: 'সম্পন্ন' },
  'jcstatus.Cancelled':   { en: 'Cancelled',   bn: 'বাতিল' },

  // ── Job card scan types (enum text from the API) ──────────────────────────
  'scan.Start':     { en: 'Start',    bn: 'শুরু' },
  'scan.Pause':     { en: 'Pause',    bn: 'বিরতি' },
  'scan.Resume':    { en: 'Resume',   bn: 'পুনরায় শুরু' },
  'scan.Complete':  { en: 'Complete', bn: 'সম্পন্ন' },
  'scan.QcCheck':   { en: 'QC Check', bn: 'কিউসি পরীক্ষা' },
  'scan.Cancel':    { en: 'Cancel',   bn: 'বাতিল' },

  // ── Job card detail screen ────────────────────────────────────────────────
  'jobcard.back':        { en: 'Back to Job Cards', bn: 'জব কার্ড তালিকায় ফিরুন' },
  'jobcard.start':       { en: 'Start',     bn: 'শুরু করুন' },
  'jobcard.pause':       { en: 'Pause',     bn: 'বিরতি দিন' },
  'jobcard.resume':      { en: 'Resume',    bn: 'আবার শুরু করুন' },
  'jobcard.complete':    { en: 'Complete',  bn: 'সম্পন্ন করুন' },
  'jobcard.cancel':      { en: 'Cancel',    bn: 'বাতিল করুন' },
  'jobcard.printCard':   { en: 'Print Card', bn: 'কার্ড প্রিন্ট করুন' },
  'jobcard.loadingQr':   { en: 'Loading QR...', bn: 'QR লোড হচ্ছে...' },
  'jobcard.po':          { en: 'PO',        bn: 'প্রোডাকশন অর্ডার' },
  'jobcard.product':     { en: 'Product',   bn: 'পণ্য' },
  'jobcard.stage':       { en: 'Stage',     bn: 'ধাপ' },
  'jobcard.batch':       { en: 'Batch',     bn: 'ব্যাচ' },
  'jobcard.qty':         { en: 'Qty',       bn: 'পরিমাণ' },
  'jobcard.quantity':    { en: 'Quantity',  bn: 'পরিমাণ' },
  'jobcard.completedQty':{ en: 'Completed', bn: 'সম্পন্ন' },
  'jobcard.rejected':    { en: 'Rejected',  bn: 'বাতিলকৃত' },
  'jobcard.activeTime':  { en: 'Active Time', bn: 'কার্যকাল' },
  'jobcard.machine':     { en: 'Machine',   bn: 'মেশিন' },
  'jobcard.operator':    { en: 'Operator',  bn: 'অপারেটর' },
  'jobcard.startedAt':   { en: 'Started',   bn: 'শুরু হয়েছে' },
  'jobcard.completedAt': { en: 'Completed', bn: 'শেষ হয়েছে' },
  'jobcard.details':     { en: 'Details',   bn: 'বিস্তারিত' },
  'jobcard.scanTimeline':{ en: 'Scan Timeline', bn: 'স্ক্যান টাইমলাইন' },
  'jobcard.noScans':     { en: 'No scans yet.', bn: 'এখনো কোনো স্ক্যান হয়নি।' },
  'jobcard.by':          { en: 'by',        bn: 'করেছেন' },
  'jobcard.loading':     { en: 'Loading...', bn: 'লোড হচ্ছে...' }
};
