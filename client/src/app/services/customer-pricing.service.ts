import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CustomerProductPriceDto,
  CreateCustomerProductPriceRequest,
  UpdateCustomerProductPriceRequest
} from '../models/customer-pricing.models';

@Injectable({ providedIn: 'root' })
export class CustomerPricingService {
  private readonly base = `${environment.apiBaseUrl}/api/customer-pricing`;

  constructor(private http: HttpClient) {}

  getForCustomer(customerId: number, includeInactive = false): Observable<ApiResponse<CustomerProductPriceDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<CustomerProductPriceDto[]>>(`${this.base}/customer/${customerId}`, { params });
  }

  /** Resolve a single customer+product price (data = null when none is set). */
  lookup(customerId: number, productId: number): Observable<ApiResponse<number | null>> {
    const params = new HttpParams().set('customerId', customerId.toString()).set('productId', productId.toString());
    return this.http.get<ApiResponse<number | null>>(`${this.base}/lookup`, { params });
  }

  create(data: CreateCustomerProductPriceRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  update(data: UpdateCustomerProductPriceRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${data.id}`, data);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }
}
