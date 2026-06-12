import { Component } from '@angular/core';
import { TranslationService } from './translation.service';

/**
 * EN | বাংলা segmented toggle for operator-facing screens. Drop into any
 * page header (host module must import SharedModule). Language persists per
 * device via TranslationService (localStorage); the impure `t` pipe makes all
 * visible bindings re-evaluate on the click's change-detection pass.
 */
@Component({
  selector: 'app-lang-toggle',
  standalone: false,
  template: `
    <div class="lang-toggle">
      <button type="button" [class.active]="i18n.lang === 'en'" (click)="i18n.setLang('en')">EN</button>
      <button type="button" [class.active]="i18n.lang === 'bn'" (click)="i18n.setLang('bn')">বাংলা</button>
    </div>
  `,
  styles: [`
    .lang-toggle {
      display: flex;
      gap: 0;
      border: 1px solid #d1d5db;
      border-radius: 8px;
      overflow: hidden;
      align-self: center;
    }
    button {
      background: white;
      border: none;
      padding: 0.45rem 0.9rem;
      font-size: 0.85rem;
      font-weight: 600;
      color: #6b7280;
      cursor: pointer;
    }
    button:first-child { border-right: 1px solid #d1d5db; }
    button.active { background: #4f46e5; color: white; }
    button:hover:not(.active) { background: #f3f4f6; }
  `]
})
export class LangToggleComponent {
  constructor(public i18n: TranslationService) {}
}
