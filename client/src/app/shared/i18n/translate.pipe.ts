import { Pipe, PipeTransform } from '@angular/core';
import { TranslationService } from './translation.service';

/**
 * `{{ 'checkin.title' | t }}` — translates via TranslationService.
 * Impure on purpose: the active language can change at runtime (toggle button)
 * without any input to the pipe changing. Lookups are a single object access,
 * so re-evaluating per change-detection pass is cheap.
 */
@Pipe({ name: 't', pure: false, standalone: false })
export class TranslatePipe implements PipeTransform {
  constructor(private i18n: TranslationService) {}

  transform(key: string | null | undefined, fallback?: string): string {
    if (!key) return fallback ?? '';
    return this.i18n.t(key, fallback);
  }
}
