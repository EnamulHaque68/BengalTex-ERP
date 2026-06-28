import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProductionOrderService } from '../../../services/production-order.service';
import { ProductionCalendarDto, ProductionCalendarEventDto } from '../../../models/production-order.models';

interface DayCell {
  date: Date;
  iso: string;
  day: number;
  inMonth: boolean;
  isToday: boolean;
  isWeekend: boolean;
  holidayName: string | null;
  events: ProductionCalendarEventDto[];
}

@Component({
  selector: 'app-production-calendar',
  standalone: false,
  templateUrl: './production-calendar.component.html',
  styleUrl: './production-calendar.component.scss'
})
export class ProductionCalendarComponent implements OnInit {
  readonly weekdayLabels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  readonly monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'];

  viewYear!: number;
  viewMonth!: number;     // 0-based
  loading = false;
  error = '';

  weeks: DayCell[][] = [];
  totalOrders = 0;

  constructor(
    private svc: ProductionOrderService,
    private router: Router,
    private zone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const now = new Date();
    this.viewYear = now.getFullYear();
    this.viewMonth = now.getMonth();
    this.load();
  }

  get monthLabel(): string {
    return `${this.monthNames[this.viewMonth]} ${this.viewYear}`;
  }

  // ── Date helpers (local, no timezone shift) ──
  private pad(n: number): string { return n < 10 ? '0' + n : '' + n; }
  private iso(d: Date): string { return `${d.getFullYear()}-${this.pad(d.getMonth() + 1)}-${this.pad(d.getDate())}`; }
  private parseIso(s: string): Date { const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d); }

  /** First/last day of the 6-week grid that contains the viewed month (weeks start Sunday). */
  private gridStart(): Date {
    const first = new Date(this.viewYear, this.viewMonth, 1);
    const start = new Date(first);
    start.setDate(first.getDate() - first.getDay());
    return start;
  }

  load(): void {
    const start = this.gridStart();
    const end = new Date(start);
    end.setDate(start.getDate() + 41);   // 6 weeks × 7 days

    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    this.svc.getCalendar(this.iso(start), this.iso(end)).subscribe({
      next: (res) => this.zone.run(() => {
        this.loading = false;
        if (res.success && res.data) this.buildGrid(res.data);
        else this.error = res.message || 'Could not load calendar.';
        this.cdr.detectChanges();
      }),
      error: (err) => this.zone.run(() => {
        this.loading = false;
        this.error = err?.error?.message || 'Could not load calendar.';
        this.cdr.detectChanges();
      })
    });
  }

  private buildGrid(data: ProductionCalendarDto): void {
    this.totalOrders = data.orders.length;
    const weekendSet = new Set(data.weekendDays);
    const holidayMap = new Map(data.holidays.map(h => [h.date, h.name]));

    // Spread each order across every day of its span.
    const ordersByDay = new Map<string, ProductionCalendarEventDto[]>();
    for (const o of data.orders) {
      const startStr = o.plannedStartDate ?? o.actualStartDate;
      if (!startStr) continue;
      const endStr = o.plannedEndDate ?? o.plannedStartDate ?? o.actualEndDate ?? o.actualStartDate ?? startStr;
      let cur = this.parseIso(startStr);
      const last = this.parseIso(endStr);
      let guard = 0;
      while (cur <= last && guard++ < 400) {
        const key = this.iso(cur);
        (ordersByDay.get(key) ?? ordersByDay.set(key, []).get(key)!).push(o);
        cur = new Date(cur); cur.setDate(cur.getDate() + 1);
      }
    }

    const todayIso = this.iso(new Date());
    const cursor = this.gridStart();
    const weeks: DayCell[][] = [];
    for (let w = 0; w < 6; w++) {
      const row: DayCell[] = [];
      for (let d = 0; d < 7; d++) {
        const iso = this.iso(cursor);
        row.push({
          date: new Date(cursor),
          iso,
          day: cursor.getDate(),
          inMonth: cursor.getMonth() === this.viewMonth,
          isToday: iso === todayIso,
          isWeekend: weekendSet.has(cursor.getDay()),
          holidayName: holidayMap.get(iso) ?? null,
          events: ordersByDay.get(iso) ?? []
        });
        cursor.setDate(cursor.getDate() + 1);
      }
      weeks.push(row);
    }
    this.weeks = weeks;
  }

  prevMonth(): void {
    if (--this.viewMonth < 0) { this.viewMonth = 11; this.viewYear--; }
    this.load();
  }
  nextMonth(): void {
    if (++this.viewMonth > 11) { this.viewMonth = 0; this.viewYear++; }
    this.load();
  }
  goToday(): void {
    const now = new Date();
    this.viewYear = now.getFullYear();
    this.viewMonth = now.getMonth();
    this.load();
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Draft': return 'st-draft';
      case 'InProgress': return 'st-progress';
      case 'Completed': return 'st-done';
      default: return 'st-other';
    }
  }

  openOrder(ev: ProductionCalendarEventDto): void {
    this.router.navigate(['/production-orders'], { queryParams: { open: ev.id } });
  }
}
