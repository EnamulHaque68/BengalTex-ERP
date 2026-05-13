import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

// PrimeNG theme
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { AuthInterceptor } from './interceptors/auth.interceptor';

@NgModule({
  declarations: [App],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptorsFromDi()),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          // Required so PrimeNG-generated layers don't override our custom rules.
          // (Without this, p-card / p-button base styles can win over component SCSS.)
          cssLayer: false,
          darkModeSelector: false
        }
      }
    }),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ],
  bootstrap: [App]
})
export class AppModule {}
