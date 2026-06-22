import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { AttendanceService } from '../../../services/attendance.service';
import {
  OfficeLocationDto, OfficeLocationEmployeeDto, UpsertOfficeLocation, OFFICE_LOCATION_TYPES
} from '../../../models/attendance.models';

@Component({
  selector: 'app-office-locations',
  standalone: false,
  templateUrl: './office-locations.component.html',
  styleUrl: './office-locations.component.scss'
})
export class OfficeLocationsComponent implements OnInit {
  types = OFFICE_LOCATION_TYPES;
  loading = true;
  error = '';
  locations: OfficeLocationDto[] = [];

  // edit dialog
  editOpen = false;
  editId: number | null = null;
  editBusy = false;
  editError = '';
  form: UpsertOfficeLocation = this.blank();
  gpsBusy = false;

  // assignment dialog
  assignOpen = false;
  assignBusy = false;
  assignError = '';
  assignLoc: OfficeLocationDto | null = null;
  assignSearch = '';
  employees: OfficeLocationEmployeeDto[] = [];

  constructor(
    private svc: AttendanceService, private zone: NgZone,
    private cdr: ChangeDetectorRef, private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void { this.load(); }

  private blank(): UpsertOfficeLocation {
    return { name: '', type: 'Factory', latitude: 0, longitude: 0, radiusMeters: 50, address: '', isActive: true };
  }

  load(): void {
    this.loading = true;
    this.svc.getOfficeLocations().subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.locations = res.data;
        else this.error = res.message || 'Could not load locations.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false; this.error = err?.error?.message || 'Could not load locations.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Create / edit ──
  openCreate(): void {
    this.editId = null; this.form = this.blank(); this.editError = ''; this.editOpen = true;
    this.cdr.detectChanges();
  }

  openEdit(l: OfficeLocationDto): void {
    this.editId = l.id;
    this.form = { name: l.name, type: l.type, latitude: l.latitude, longitude: l.longitude,
      radiusMeters: l.radiusMeters, address: l.address, isActive: l.isActive };
    this.editError = ''; this.editOpen = true;
    this.cdr.detectChanges();
  }

  useMyLocation(): void {
    if (!('geolocation' in navigator)) { this.editError = 'GPS not available on this device.'; return; }
    this.gpsBusy = true; this.cdr.detectChanges();
    navigator.geolocation.getCurrentPosition(
      (pos) => this.zone.run(() => {
        this.form.latitude = +pos.coords.latitude.toFixed(6);
        this.form.longitude = +pos.coords.longitude.toFixed(6);
        this.gpsBusy = false; this.cdr.detectChanges();
      }),
      () => this.zone.run(() => { this.gpsBusy = false; this.editError = 'Could not get your location.'; this.cdr.detectChanges(); }),
      { enableHighAccuracy: true, timeout: 15000 }
    );
  }

  saveLocation(): void {
    if (this.editBusy) return;
    if (!this.form.name.trim()) { this.editError = 'Name is required.'; this.cdr.detectChanges(); return; }
    this.editBusy = true; this.editError = '';
    const op = this.editId
      ? this.svc.updateOfficeLocation(this.editId, this.form)
      : this.svc.createOfficeLocation(this.form);
    op.subscribe({
      next: (res) => this.zone.run(() => {
        this.editBusy = false;
        if (res.success) { this.editOpen = false; this.load(); }
        else this.editError = res.message || 'Could not save.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.editBusy = false; this.editError = err?.error?.message || 'Could not save.';
        this.cdr.detectChanges();
      })
    });
  }

  remove(l: OfficeLocationDto): void {
    if (!confirm(`Delete "${l.name}"? Employee assignments to it will be removed.`)) return;
    this.svc.deleteOfficeLocation(l.id).subscribe({
      next: () => this.zone.run(() => { this.load(); this.cdr.detectChanges(); }),
      error: (err) => this.zone.run(() => { this.error = err?.error?.message || 'Could not delete.'; this.cdr.detectChanges(); })
    });
  }

  // ── Employee assignment ──
  openAssign(l: OfficeLocationDto): void {
    this.assignLoc = l; this.assignOpen = true; this.assignError = ''; this.assignSearch = ''; this.employees = [];
    this.cdr.detectChanges();
    this.svc.getOfficeLocationEmployees(l.id).subscribe({
      next: (res) => this.zone.run(() => {
        if (res.success && res.data) this.employees = res.data;
        this.cdr.detectChanges();
      }),
      error: () => {}
    });
  }

  get filteredEmployees(): OfficeLocationEmployeeDto[] {
    const q = this.assignSearch.trim().toLowerCase();
    if (!q) return this.employees;
    return this.employees.filter(e =>
      e.employeeName.toLowerCase().includes(q) || e.employeeCode.toLowerCase().includes(q) ||
      (e.department || '').toLowerCase().includes(q));
  }

  get assignedCount(): number { return this.employees.filter(e => e.assigned).length; }

  toggleAll(on: boolean): void { this.filteredEmployees.forEach(e => e.assigned = on); }

  saveAssign(): void {
    if (!this.assignLoc || this.assignBusy) return;
    this.assignBusy = true; this.assignError = '';
    const ids = this.employees.filter(e => e.assigned).map(e => e.employeeId);
    this.svc.setOfficeLocationEmployees(this.assignLoc.id, ids).subscribe({
      next: (res) => this.zone.run(() => {
        this.assignBusy = false;
        if (res.success) { this.assignOpen = false; this.load(); }
        else this.assignError = res.message || 'Could not save.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.assignBusy = false; this.assignError = err?.error?.message || 'Could not save.';
        this.cdr.detectChanges();
      })
    });
  }

  // ── Map preview in the edit dialog ──
  get mapUrl(): SafeResourceUrl | null {
    const { latitude: lat, longitude: lon } = this.form;
    if (!lat && !lon) return null;
    const d = 0.0025;
    const bbox = `${(lon - d).toFixed(5)},${(lat - d).toFixed(5)},${(lon + d).toFixed(5)},${(lat + d).toFixed(5)}`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(
      `https://www.openstreetmap.org/export/embed.html?bbox=${bbox}&layer=mapnik&marker=${lat},${lon}`);
  }

  typeLabel(v: string): string { return this.types.find(t => t.value === v)?.label ?? v; }
}
