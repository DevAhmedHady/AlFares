import { Component, output } from '@angular/core';

/** Lightweight centered modal with a title slot and projected body. */
@Component({
  selector: 'app-modal',
  standalone: true,
  template: `
    <div class="overlay" (click)="closed.emit()">
      <div class="dialog card" (click)="$event.stopPropagation()">
        <ng-content />
      </div>
    </div>
  `,
  styles: [`
    .overlay { position: fixed; inset: 0; background: rgba(15,23,42,0.45); display: grid; place-items: center; z-index: 50; }
    .dialog { width: 480px; max-width: 92vw; max-height: 90vh; overflow: auto; }
  `],
})
export class ModalComponent {
  readonly closed = output<void>();
}
