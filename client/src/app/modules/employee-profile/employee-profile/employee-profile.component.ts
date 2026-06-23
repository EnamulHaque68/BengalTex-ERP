import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EmployeeService } from '../../../services/employee.service';
import { UserService } from '../../../services/user.service';
import { AvatarRefreshService } from '../../../services/avatar-refresh.service';
import { EmployeeProfileDto, ProfileSkillDto, ProfileActivityDto, MARITAL_STATUSES, BLOOD_GROUPS } from '../../../models/employee-profile.models';
import { EmployeeListItemDto } from '../../../models/employee.models';
import { UserListItemDto } from '../../../models/user.models';

interface DonutSeg { label: string; value: number; color: string; dash: number; offset: number; }

type ProfileTab = 'overview' | 'personal' | 'job' | 'education' | 'documents' | 'bank' | 'emergency' | 'skills' | 'activity';

@Component({
  selector: 'app-employee-profile',
  standalone: false,
  templateUrl: './employee-profile.component.html',
  styleUrl: './employee-profile.component.scss'
})
export class EmployeeProfileComponent implements OnInit {
  profile: EmployeeProfileDto | null = null;
  loading = false;
  error = '';
  mode: 'me' | 'id' = 'id';

  activeTab: ProfileTab = 'overview';
  readonly tabs: { key: ProfileTab; label: string; ready: boolean }[] = [
    { key: 'overview', label: 'Overview', ready: true },
    { key: 'personal', label: 'Personal Information', ready: true },
    { key: 'job', label: 'Job Information', ready: true },
    { key: 'education', label: 'Education', ready: true },
    { key: 'documents', label: 'Documents', ready: true },
    { key: 'bank', label: 'Bank Information', ready: true },
    { key: 'emergency', label: 'Emergency Contact', ready: true },
    { key: 'skills', label: 'Skills & Certificates', ready: false },
    { key: 'activity', label: 'Activity Log', ready: true }
  ];

  // Edit dialog
  editVisible = false;
  editSaving = false;
  editError = '';
  form!: FormGroup;

  // Skills manager
  skillsVisible = false;
  skills: ProfileSkillDto[] = [];
  skillsError = '';
  skillSaving = false;
  skillForm!: FormGroup;
  skillEditingId: number | null = null;

  // Education + Emergency-contact inline editors
  eduForm!: FormGroup;
  eduEditingId: number | null = null;
  eduSaving = false;
  eduError = '';
  contactForm!: FormGroup;
  contactEditingId: number | null = null;
  contactSaving = false;
  contactError = '';
  readonly maritalStatuses = MARITAL_STATUSES;
  readonly bloodGroups = BLOOD_GROUPS;
  employees: EmployeeListItemDto[] = [];
  users: UserListItemDto[] = [];

  // ID card / media
  photoUrl: SafeUrl | null = null;
  qrUrl: SafeUrl | null = null;
  uploading = false;
  qrDialogVisible = false;
  idTemplate: 'blue' | 'dark' | 'gradient' = 'blue';
  readonly idTemplates: { key: 'blue' | 'dark' | 'gradient'; label: string }[] = [
    { key: 'blue', label: 'Corporate Blue' },
    { key: 'dark', label: 'Modern Dark' },
    { key: 'gradient', label: 'Premium Gradient' }
  ];

  constructor(
    private route: ActivatedRoute,
    private svc: EmployeeService,
    private userService: UserService,
    private avatarRefresh: AvatarRefreshService,
    private sanitizer: DomSanitizer,
    private fb: FormBuilder,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.skillForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      proficiencyPercent: [70, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
    this.eduForm = this.fb.group({
      degree: ['', [Validators.required, Validators.maxLength(200)]],
      institute: ['', Validators.maxLength(200)],
      passingYear: [null as number | null],
      result: ['', Validators.maxLength(100)]
    });
    this.contactForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      relationship: ['', Validators.maxLength(50)],
      phone: ['', [Validators.required, Validators.maxLength(30)]],
      address: ['', Validators.maxLength(500)]
    });
    this.mode = (this.route.snapshot.data['mode'] === 'me') ? 'me' : 'id';
    this.load();
  }

  private buildForm(): void {
    this.form = this.fb.group({
      photoUrl: ['', Validators.maxLength(500)],
      bloodGroup: [null as string | null],
      maritalStatus: ['Single', Validators.required],
      religion: ['', Validators.maxLength(50)],
      nationality: ['', Validators.maxLength(100)],
      workLocation: ['', Validators.maxLength(150)],
      aboutMe: ['', Validators.maxLength(1000)],
      probationEndDate: [null as string | null],
      confirmationDate: [null as string | null],
      reportingToEmployeeId: [null as number | null],
      userId: [null as string | null]
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();
    const obs = this.mode === 'me'
      ? this.svc.getMyProfile()
      : this.svc.getProfile(Number(this.route.snapshot.paramMap.get('id')));
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) { this.profile = res.data; this.fetchMedia(); }
        else this.error = res.message || 'Profile not found.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.loading = false; this.error = err?.error?.message || 'Failed to load profile.'; this.cdr.detectChanges(); })
    });
  }

  private fetchMedia(): void {
    if (!this.profile) return;
    const id = this.profile.id;
    this.svc.getQrBlob(id).subscribe({
      next: (b) => this.zone.run(() => { this.qrUrl = this.sanitizer.bypassSecurityTrustUrl(URL.createObjectURL(b)); this.cdr.detectChanges(); })
    });
    if (this.profile.photoUrl) {
      this.svc.getPhotoBlob(id).subscribe({
        next: (b) => this.zone.run(() => { this.photoUrl = this.sanitizer.bypassSecurityTrustUrl(URL.createObjectURL(b)); this.cdr.detectChanges(); }),
        error: () => this.zone.run(() => { this.photoUrl = null; this.cdr.detectChanges(); })
      });
    } else {
      this.photoUrl = null;
    }
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.profile || this.uploading) return;
    this.uploading = true;
    this.error = '';
    this.cdr.detectChanges();
    this.svc.uploadPhoto(this.profile.id, file).subscribe({
      next: (res) => this.zone.run(() => {
        this.uploading = false;
        if (res.success) { this.load(); this.avatarRefresh.notify(); }   // refresh topbar avatar instantly
        else this.error = res.message || 'Photo upload failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.uploading = false; this.error = err?.error?.message || 'Photo upload failed.'; this.cdr.detectChanges(); })
    });
    input.value = '';
  }

  printIdCard(): void {
    window.print();
  }

  viewIdCard(): void {
    this.activeTab = 'overview';
    this.cdr.detectChanges();
    setTimeout(() => document.querySelector('.id-section')?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 60);
  }

  setTab(t: ProfileTab): void {
    this.activeTab = t;
    if (t === 'activity' && !this.activityLoaded) this.loadActivity();
  }

  // Activity log
  activity: ProfileActivityDto[] = [];
  activityLoading = false;
  activityLoaded = false;

  private loadActivity(): void {
    if (!this.profile) return;
    this.activityLoading = true;
    this.cdr.detectChanges();
    this.svc.getActivity(this.profile.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.activityLoading = false;
        this.activityLoaded = true;
        if (res.success && res.data) this.activity = res.data.items;
        this.cdr.detectChanges();
      }),
      error: () => this.zone.run(() => { this.activityLoading = false; this.activityLoaded = true; this.cdr.detectChanges(); })
    });
  }

  initials(name: string): string {
    return (name || '?').split(' ').filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }

  leavePct(b: { taken: number; entitled: number }): number {
    return b.entitled > 0 ? Math.min(100, Math.round((b.taken / b.entitled) * 100)) : 0;
  }

  // ── Attendance donut (SVG) ── circumference for r=54 ≈ 339.29
  readonly donutCirc = 2 * Math.PI * 54;
  donutSegments(): DonutSeg[] {
    const a = this.profile?.attendance;
    if (!a) return [];
    const items = [
      { label: 'Present', value: a.presentDays, color: '#10b981' },
      { label: 'Late', value: a.lateDays, color: '#f59e0b' },
      { label: 'Absent', value: a.absentDays, color: '#ef4444' },
      { label: 'Leave', value: a.leaveDays, color: '#9ca3af' }
    ];
    const total = items.reduce((s, i) => s + i.value, 0);
    if (total === 0) return [];
    let acc = 0;
    return items.filter(i => i.value > 0).map(i => {
      const dash = (i.value / total) * this.donutCirc;
      const seg: DonutSeg = { ...i, dash, offset: -(acc / total) * this.donutCirc };
      acc += i.value;
      return seg;
    });
  }

  // ── Salary history line (SVG) ── chronological points from the latest payslips
  get salaryChrono() {
    return this.profile ? [...this.profile.latestPayslips].reverse() : [];
  }
  salaryLine(): { points: string; dots: { x: number; y: number }[]; max: number } {
    const data = this.salaryChrono;
    if (data.length === 0) return { points: '', dots: [], max: 0 };
    const w = 320, h = 90, padX = 10, padY = 12;
    const max = Math.max(...data.map(d => d.netPay), 1);
    const min = Math.min(...data.map(d => d.netPay));
    const range = Math.max(max - min, 1);
    const stepX = data.length > 1 ? (w - padX * 2) / (data.length - 1) : 0;
    const dots = data.map((d, i) => ({
      x: padX + i * stepX,
      y: padY + (h - padY * 2) * (1 - (d.netPay - min) / range)
    }));
    return { points: dots.map(p => `${p.x},${p.y}`).join(' '), dots, max };
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-BD', { style: 'currency', currency: 'BDT', maximumFractionDigits: 0 }).format(amount || 0);
  }

  // ── Edit ──
  openEdit(): void {
    if (!this.profile) return;
    this.editError = '';
    const p = this.profile;
    this.form.reset({
      photoUrl: p.photoUrl ?? '',
      bloodGroup: p.bloodGroup ?? null,
      maritalStatus: p.maritalStatus || 'Single',
      religion: p.religion ?? '',
      nationality: p.nationality ?? '',
      workLocation: p.workLocation ?? '',
      aboutMe: p.aboutMe ?? '',
      probationEndDate: p.probationEndDate ?? null,
      confirmationDate: p.confirmationDate ?? null,
      reportingToEmployeeId: p.reportingToEmployeeId ?? null,
      userId: p.userId ?? null
    });
    this.editVisible = true;
    if (this.employees.length === 0) this.loadEditPickers();
  }

  private loadEditPickers(): void {
    this.svc.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.employees = res.data.items.filter(e => e.id !== this.profile?.id); this.cdr.detectChanges(); })
    });
    this.userService.getAll({ page: 1, pageSize: 1000, search: '' }).subscribe({
      next: (res) => this.zone.run(() => { if (res.success && res.data) this.users = res.data.items; this.cdr.detectChanges(); })
    });
  }

  saveEdit(): void {
    if (!this.profile || this.form.invalid || this.editSaving) return;
    this.editSaving = true;
    this.editError = '';
    this.cdr.detectChanges();
    const v = this.form.getRawValue();
    this.svc.updateProfile(this.profile.id, {
      employeeId: this.profile.id,
      photoUrl: (v.photoUrl as string)?.trim() || null,
      bloodGroup: v.bloodGroup || null,
      maritalStatus: v.maritalStatus,
      religion: (v.religion as string)?.trim() || null,
      nationality: (v.nationality as string)?.trim() || null,
      workLocation: (v.workLocation as string)?.trim() || null,
      aboutMe: (v.aboutMe as string)?.trim() || null,
      probationEndDate: v.probationEndDate || null,
      confirmationDate: v.confirmationDate || null,
      reportingToEmployeeId: v.reportingToEmployeeId || null,
      userId: v.userId || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.editSaving = false;
        if (res.success) { this.editVisible = false; this.load(); }
        else this.editError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.editSaving = false; this.editError = err?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Skills manager ──
  openSkills(): void {
    if (!this.profile) return;
    this.skills = [...this.profile.skills];
    this.skillsError = '';
    this.resetSkillForm();
    this.skillsVisible = true;
  }

  private resetSkillForm(): void {
    this.skillEditingId = null;
    this.skillForm.reset({ name: '', proficiencyPercent: 70 });
  }

  editSkill(s: ProfileSkillDto): void {
    this.skillEditingId = s.id;
    this.skillForm.patchValue({ name: s.name, proficiencyPercent: s.proficiencyPercent });
    this.cdr.detectChanges();
  }

  private reloadSkills(): void {
    if (!this.profile) return;
    this.svc.getSkills(this.profile.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) { this.skills = res.data; if (this.profile) this.profile.skills = res.data; }
        this.cdr.detectChanges();
      })
    });
  }

  saveSkill(): void {
    if (this.skillForm.invalid || this.skillSaving || !this.profile) return;
    this.skillSaving = true;
    this.skillsError = '';
    this.cdr.detectChanges();
    const v = this.skillForm.getRawValue();
    const body = { name: (v.name as string).trim(), proficiencyPercent: Number(v.proficiencyPercent) || 0 };
    const obs = this.skillEditingId ? this.svc.updateSkill(this.skillEditingId, body) : this.svc.addSkill(this.profile.id, body);
    obs.subscribe({
      next: (res) => this.zone.run(() => {
        this.skillSaving = false;
        if (res.success) { this.resetSkillForm(); this.reloadSkills(); }
        else this.skillsError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.skillSaving = false; this.skillsError = err?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }

  deleteSkill(s: ProfileSkillDto): void {
    if (this.skillSaving) return;
    this.skillSaving = true;
    this.cdr.detectChanges();
    this.svc.deleteSkill(s.id).subscribe({
      next: (res) => this.zone.run(() => {
        this.skillSaving = false;
        if (res.success) { if (this.skillEditingId === s.id) this.resetSkillForm(); this.reloadSkills(); }
        else this.skillsError = res.message || 'Delete failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.skillSaving = false; this.skillsError = err?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Education ──
  editEducation(e: any): void {
    this.eduEditingId = e.id;
    this.eduForm.patchValue({ degree: e.degree, institute: e.institute ?? '', passingYear: e.passingYear, result: e.result ?? '' });
    this.cdr.detectChanges();
  }
  cancelEdu(): void { this.eduEditingId = null; this.eduForm.reset({ degree: '', institute: '', passingYear: null, result: '' }); this.cdr.detectChanges(); }
  saveEducation(): void {
    if (this.eduForm.invalid || this.eduSaving || !this.profile) return;
    this.eduSaving = true; this.eduError = ''; this.cdr.detectChanges();
    const v = this.eduForm.getRawValue();
    this.svc.saveEducation(this.profile.id, {
      id: this.eduEditingId ?? 0, degree: (v.degree as string).trim(),
      institute: (v.institute as string)?.trim() || null,
      passingYear: v.passingYear ? Number(v.passingYear) : null, result: (v.result as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.eduSaving = false;
        if (res.success) { this.cancelEdu(); this.load(); } else this.eduError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.eduSaving = false; this.eduError = err?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }
  deleteEducation(e: any): void {
    if (this.eduSaving) return;
    this.eduSaving = true; this.cdr.detectChanges();
    this.svc.deleteEducation(e.id).subscribe({
      next: (res) => this.zone.run(() => { this.eduSaving = false; if (res.success) this.load(); else this.eduError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (err) => this.zone.run(() => { this.eduSaving = false; this.eduError = err?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }

  // ── Emergency contacts ──
  editContact(c: any): void {
    this.contactEditingId = c.id;
    this.contactForm.patchValue({ name: c.name, relationship: c.relationship ?? '', phone: c.phone, address: c.address ?? '' });
    this.cdr.detectChanges();
  }
  cancelContact(): void { this.contactEditingId = null; this.contactForm.reset({ name: '', relationship: '', phone: '', address: '' }); this.cdr.detectChanges(); }
  saveContact(): void {
    if (this.contactForm.invalid || this.contactSaving || !this.profile) return;
    this.contactSaving = true; this.contactError = ''; this.cdr.detectChanges();
    const v = this.contactForm.getRawValue();
    this.svc.saveContact(this.profile.id, {
      id: this.contactEditingId ?? 0, name: (v.name as string).trim(),
      relationship: (v.relationship as string)?.trim() || null, phone: (v.phone as string).trim(),
      address: (v.address as string)?.trim() || null
    }).subscribe({
      next: (res) => this.zone.run(() => {
        this.contactSaving = false;
        if (res.success) { this.cancelContact(); this.load(); } else this.contactError = res.message || 'Save failed.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => { this.contactSaving = false; this.contactError = err?.error?.message || 'Save failed.'; this.cdr.detectChanges(); })
    });
  }
  deleteContact(c: any): void {
    if (this.contactSaving) return;
    this.contactSaving = true; this.cdr.detectChanges();
    this.svc.deleteContact(c.id).subscribe({
      next: (res) => this.zone.run(() => { this.contactSaving = false; if (res.success) this.load(); else this.contactError = res.message || 'Delete failed.'; this.cdr.detectChanges(); }),
      error: (err) => this.zone.run(() => { this.contactSaving = false; this.contactError = err?.error?.message || 'Delete failed.'; this.cdr.detectChanges(); })
    });
  }
}
