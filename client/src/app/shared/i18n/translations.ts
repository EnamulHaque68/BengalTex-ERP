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
  'jobcard.loading':     { en: 'Loading...', bn: 'লোড হচ্ছে...' },

  // ── QR scanner screen ─────────────────────────────────────────────────────
  'scanner.title':        { en: 'QR Scanner', bn: 'QR স্ক্যানার' },
  'scanner.subtitle':     {
    en: 'Pick action below, then point camera at job-card QR codes to apply continuously.',
    bn: 'নিচে অ্যাকশন বেছে নিন, তারপর জব-কার্ডের QR কোডে ক্যামেরা ধরুন — একটানা প্রয়োগ হবে।'
  },
  'scanner.startCamera':  { en: 'Start Camera', bn: 'ক্যামেরা চালু করুন' },
  'scanner.stopCamera':   { en: 'Stop Camera',  bn: 'ক্যামেরা বন্ধ করুন' },
  'scanner.noPermission': {
    en: "You don't have permission to scan job cards (JobCards.Scan).",
    bn: 'জব কার্ড স্ক্যান করার অনুমতি আপনার নেই (JobCards.Scan)।'
  },
  'scanner.actionOnScan': { en: 'Action on each scan:', bn: 'প্রতি স্ক্যানে অ্যাকশন:' },
  'scanner.clickStart':   {
    en: 'Click "Start Camera" to begin scanning.',
    bn: '"ক্যামেরা চালু করুন" চাপুন স্ক্যান শুরু করতে।'
  },
  'scanner.posting':      { en: 'Posting scan...', bn: 'স্ক্যান পাঠানো হচ্ছে...' },
  'scanner.history':      { en: 'Scan History', bn: 'স্ক্যান ইতিহাস' },
  'scanner.clear':        { en: 'Clear', bn: 'মুছুন' },
  'scanner.empty':        {
    en: 'No scans yet. Aim the camera at a job-card QR code.',
    bn: 'এখনো কোনো স্ক্যান হয়নি। জব-কার্ডের QR কোডে ক্যামেরা ধরুন।'
  },

  // ── Leave statuses (enum text from the API) ───────────────────────────────
  'lstatus.Pending':    { en: 'Pending',   bn: 'অপেক্ষমাণ' },
  'lstatus.Approved':   { en: 'Approved',  bn: 'অনুমোদিত' },
  'lstatus.Rejected':   { en: 'Rejected',  bn: 'প্রত্যাখ্যাত' },
  'lstatus.Cancelled':  { en: 'Cancelled', bn: 'বাতিল' },

  // ── Leave applications screen ─────────────────────────────────────────────
  'leave.title':        { en: 'Leave Applications', bn: 'ছুটির আবেদন' },
  'leave.subtitle':     {
    en: 'Apply for leave, approve / reject pending requests, see history',
    bn: 'ছুটির আবেদন করুন, অনুমোদন / প্রত্যাখ্যান করুন, ইতিহাস দেখুন'
  },
  'leave.types':        { en: 'Types',    bn: 'ধরন' },
  'leave.holidays':     { en: 'Holidays', bn: 'ছুটির দিন' },
  'leave.balances':     { en: 'Balances', bn: 'ব্যালেন্স' },
  'leave.apply':        { en: 'Apply Leave', bn: 'ছুটির আবেদন করুন' },
  'leave.searchPlaceholder': { en: 'Search code, employee...', bn: 'কোড বা কর্মীর নাম খুঁজুন...' },
  'leave.allStatuses':  { en: 'All statuses',  bn: 'সব অবস্থা' },
  'leave.allEmployees': { en: 'All employees', bn: 'সব কর্মী' },
  'leave.code':         { en: 'Code',     bn: 'কোড' },
  'leave.employee':     { en: 'Employee', bn: 'কর্মী' },
  'leave.type':         { en: 'Type',     bn: 'ধরন' },
  'leave.from':         { en: 'From',     bn: 'থেকে' },
  'leave.to':           { en: 'To',       bn: 'পর্যন্ত' },
  'leave.days':         { en: 'Days',     bn: 'দিন' },
  'leave.reason':       { en: 'Reason',   bn: 'কারণ' },
  'leave.actions':      { en: 'Actions',  bn: 'পদক্ষেপ' },
  'leave.empty':        { en: 'No leave applications.', bn: 'কোনো ছুটির আবেদন নেই।' },
  'leave.approve':      { en: 'Approve', bn: 'অনুমোদন' },
  'leave.rejectTip':    { en: 'Reject',  bn: 'প্রত্যাখ্যান' },
  'leave.cancelTip':    { en: 'Cancel',  bn: 'বাতিল' },
  'leave.applyTitle':   { en: 'Apply for Leave', bn: 'ছুটির আবেদন' },
  'leave.leaveType':    { en: 'Leave Type', bn: 'ছুটির ধরন' },
  'leave.fromDate':     { en: 'From Date',  bn: 'শুরুর তারিখ' },
  'leave.toDate':       { en: 'To Date',    bn: 'শেষ তারিখ' },
  'leave.reasonPlaceholder': {
    en: 'e.g. Family function, medical appointment',
    bn: 'যেমন: পারিবারিক অনুষ্ঠান, ডাক্তার দেখানো'
  },
  'leave.autoAttendance': { en: 'Auto-write Attendance (recommended)', bn: 'হাজিরা স্বয়ংক্রিয় লেখা (প্রস্তাবিত)' },
  'leave.autoAttendanceHint': {
    en: 'On approve, AttendanceRecord rows for working days will be set to "Leave". Disable if entering attendance manually.',
    bn: 'অনুমোদনের পর কর্মদিবসগুলোর হাজিরা স্বয়ংক্রিয়ভাবে "ছুটি" হিসেবে লেখা হবে। হাজিরা নিজে লিখলে বন্ধ রাখুন।'
  },
  'leave.cancelBtn':    { en: 'Cancel',  bn: 'বাতিল' },
  'leave.submit':       { en: 'Submit',  bn: 'জমা দিন' },
  'leave.submitting':   { en: 'Submitting...', bn: 'জমা হচ্ছে...' },
  'leave.rejectTitle':  { en: 'Reject Leave', bn: 'ছুটি প্রত্যাখ্যান' },
  'leave.rejectConfirm': { en: 'Reject this leave application?', bn: 'এই ছুটির আবেদন প্রত্যাখ্যান করবেন?' },
  'leave.rejectionReason': { en: 'Rejection Reason', bn: 'প্রত্যাখ্যানের কারণ' },
  'leave.rejectBtn':    { en: 'Reject', bn: 'প্রত্যাখ্যান করুন' },

  // ── Login screen ──────────────────────────────────────────────────────────
  'login.tagline':      { en: 'Garments Accessories Manufacturing', bn: 'গার্মেন্টস এক্সেসরিজ ম্যানুফ্যাকচারিং' },
  'login.title':        { en: 'Sign in to your account', bn: 'আপনার অ্যাকাউন্টে সাইন ইন করুন' },
  'login.userLabel':    { en: 'Email or Username', bn: 'ইমেইল বা ইউজারনেম' },
  'login.userRequired': { en: 'Email or username is required.', bn: 'ইমেইল বা ইউজারনেম দিতে হবে।' },
  'login.password':     { en: 'Password', bn: 'পাসওয়ার্ড' },
  'login.passwordPlaceholder': { en: 'Enter your password', bn: 'আপনার পাসওয়ার্ড লিখুন' },
  'login.passwordRequired': { en: 'Password is required.', bn: 'পাসওয়ার্ড দিতে হবে।' },
  'login.signIn':       { en: 'Sign In', bn: 'সাইন ইন' },
  'login.forgot':       { en: 'Forgot your password?', bn: 'পাসওয়ার্ড ভুলে গেছেন?' }
};
