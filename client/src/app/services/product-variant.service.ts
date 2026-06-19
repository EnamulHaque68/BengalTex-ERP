import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  ProductVariantDto,
  CreateProductVariantRequest,
  UpdateProductVariantRequest,
  BulkCreateProductVariantsRequest
} from '../models/product-variant.models';

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly base = `${environment.apiBaseUrl}/api/product-variants`;

  constructor(private http: HttpClient) {}

  getByProduct(productId: number): Observable<ApiResponse<ProductVariantDto[]>> {
    const params = new HttpParams().set('productId', productId.toString());
    return this.http.get<ApiResponse<ProductVariantDto[]>>(this.base, { params });
  }

  create(data: CreateProductVariantRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(this.base, data);
  }

  bulkCreate(data: BulkCreateProductVariantsRequest): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${this.base}/bulk`, data);
  }

  update(id: number, data: UpdateProductVariantRequest): Observable<ApiResponse<number>> {
    return this.http.put<ApiResponse<number>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<unknown>> {
    return this.http.delete<ApiResponse<unknown>>(`${this.base}/${id}`);
  }
}
