import { Component } from '@angular/core';

export interface ConfirmDialogData {
  title: string;
  message: string;
}

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <p-dialog [(visible)]="visible" [header]="title" [modal]="true">
      <p>{{ message }}</p>
      <ng-template pTemplate="footer">
        <button pButton label="Cancel" (click)="onCancel()"></button>
        <button pButton label="Confirm" (click)="onConfirm()"></button>
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    p {
      margin-bottom: 1rem;
    }
  `]
})
export class ConfirmDialogComponent {
  visible = false;
  title = '';
  message = '';

  onConfirm(): void {
    this.visible = false;
  }

  onCancel(): void {
    this.visible = false;
  }
}
