import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/auth.models';
import {
  CreateProductCategoryRequest,
  ProductCategoryDto,
  UpdateProductCategoryRequest
} from '../models/product.models';

@Injectable({ providedIn: 'root' })
export class ProductCategoryService {
  private readonly base = `${environment.apiBaseUrl}/api/product-categories`;

  constructor(private http: HttpClient) {}

  getAll(includeInactive = false): Observable<ApiResponse<ProductCategoryDto[]>> {
    return this.http.get<ApiResponse<ProductCategoryDto[]>>(this.base, {
      params: new HttpParams().set('includeInactive', includeInactive.toString())
    });
  }

  getById(id: number): Observable<ApiResponse<ProductCategoryDto>> {
    return this.http.get<ApiResponse<ProductCategoryDto>>(`${this.base}/${id}`);
  }

  create(data: CreateProductCategoryRequest): Observable<ApiResponse<ProductCategoryDto>> {
    return this.http.post<ApiResponse<ProductCategoryDto>>(this.base, data);
  }

  update(id: number, data: UpdateProductCategoryRequest): Observable<ApiResponse<ProductCategoryDto>> {
    return this.http.put<ApiResponse<ProductCategoryDto>>(`${this.base}/${id}`, data);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.base}/${id}`);
  }
}
