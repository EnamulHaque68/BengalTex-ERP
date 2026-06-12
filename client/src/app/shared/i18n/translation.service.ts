import { Injectable } from '@angular/core';
import { Lang, TRANSLATIONS } from './translations';

const STORAGE_KEY = 'btx-lang';

/**
 * Minimal runtime translation service for operator-facing screens.
 * No external dependency: the dictionary ships in the bundle, language toggles
 * instantly (the `t` pipe is impure so bindings re-evaluate on change detection),
 * and the choice persists per device via localStorage — right model for a shared
 * shop-floor tablet that stays in Bangla.
 */
@Injectable({ providedIn: 'root' })
export class TranslationService {
  lang: Lang = this.readStored();

  setLang(lang: Lang): void {
    this.lang = lang;
    try { localStorage.setItem(STORAGE_KEY, lang); } catch { /* private mode etc. */ }
  }

  /** Translate a key; unknown keys fall back to `fallback` (or the key itself). */
  t(key: string, fallback?: string): string {
    const entry = TRANSLATIONS[key];
    if (!entry) return fallback ?? key;
    return entry[this.lang] || entry.en;
  }

  private readStored(): Lang {
    try {
      return localStorage.getItem(STORAGE_KEY) === 'bn' ? 'bn' : 'en';
    } catch {
      return 'en';
    }
  }
}
