import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LoanBonusService } from './loan-bonus.service';
import { environment } from '../../environments/environment';
import {
  CreateEmployeeLoanRequest,
  UpdateEmployeeLoanRequest,
  BulkCreateFestivalBonusRequest
} from '../models/loan-bonus.models';

describe('LoanBonusService', () => {
  let service: LoanBonusService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        LoanBonusService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(LoanBonusService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Loans API', () => {
    it('getLoans should issue GET with query params', () => {
      service.getLoans({ page: 1, pageSize: 10, search: 'Karim' }, 'Active', 5).subscribe();

      const req = httpTesting.expectOne((r) =>
        r.url === `${environment.apiBaseUrl}/api/employee-loans` &&
        r.params.get('Page') === '1' &&
        r.params.get('PageSize') === '10' &&
        r.params.get('Search') === 'Karim' &&
        r.params.get('status') === 'Active' &&
        r.params.get('employeeId') === '5'
      );
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: { items: [], totalCount: 0, page: 1, pageSize: 10 } });
    });

    it('createLoan should issue POST with loan payload', () => {
      const payload: CreateEmployeeLoanRequest = {
        employeeId: 10,
        issuedDate: '2026-05-01',
        principal: 50000,
        emiAmount: 5000,
        tenureMonths: 10,
        startYearMonth: 202605,
        notes: 'Medical loan',
        disbursementMethod: 'BankTransfer'
      };

      service.createLoan(payload).subscribe((res) => {
        expect(res.data).toBe(101);
      });

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/employee-loans`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush({ success: true, data: 101 });
    });

    it('updateLoan should issue PUT with updated fields', () => {
      const payload: UpdateEmployeeLoanRequest = {
        emiAmount: 6000,
        tenureMonths: 8,
        startYearMonth: 202606,
        notes: 'Restructured'
      };

      service.updateLoan(42, payload).subscribe();

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/employee-loans/42`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(payload);
      req.flush({ success: true, data: null });
    });

    it('closeLoan should issue POST with cancel parameter', () => {
      service.closeLoan(42, true).subscribe();

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/employee-loans/42/close?cancel=true`);
      expect(req.request.method).toBe('POST');
      req.flush({ success: true, data: null });
    });
  });

  describe('Festival Bonuses API', () => {
    it('getBonuses should issue GET with query params', () => {
      service.getBonuses({ page: 2, pageSize: 20 }, 2026, 'EidUlFitr', 'Draft', 12).subscribe();

      const req = httpTesting.expectOne((r) =>
        r.url === `${environment.apiBaseUrl}/api/festival-bonuses` &&
        r.params.get('Page') === '2' &&
        r.params.get('year') === '2026' &&
        r.params.get('bonusType') === 'EidUlFitr' &&
        r.params.get('status') === 'Draft' &&
        r.params.get('employeeId') === '12'
      );
      expect(req.request.method).toBe('GET');
      req.flush({ success: true, data: { items: [], totalCount: 0, page: 2, pageSize: 20 } });
    });

    it('bulkCreateBonus should issue POST to bulk-create endpoint', () => {
      const payload: BulkCreateFestivalBonusRequest = {
        bonusYear: 2026,
        bonusType: 'EidUlAzha',
        amount: 15000,
        notes: 'Eid 2026'
      };

      service.bulkCreateBonus(payload).subscribe((res) => {
        expect(res.data).toBe(45);
      });

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/festival-bonuses/bulk-create`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(payload);
      req.flush({ success: true, data: 45 });
    });

    it('pay should issue POST to /pay endpoint', () => {
      service.pay(15).subscribe();

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/festival-bonuses/15/pay`);
      expect(req.request.method).toBe('POST');
      req.flush({ success: true, data: null });
    });

    it('deleteBonus should issue DELETE endpoint', () => {
      service.deleteBonus(15).subscribe();

      const req = httpTesting.expectOne(`${environment.apiBaseUrl}/api/festival-bonuses/15`);
      expect(req.request.method).toBe('DELETE');
      req.flush({ success: true, data: null });
    });
  });
});
