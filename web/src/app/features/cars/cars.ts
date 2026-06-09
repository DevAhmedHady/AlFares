import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { CarsService } from '../../core/api/resources';
import { GridFieldType } from '../../core/grid.models';
import { carTypeLabels, optionsFrom } from '../../core/labels';
import { CarResponse, CarType } from '../../core/models';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';

@Component({
  standalone: true,
  imports: [FormsModule, ButtonModule, DialogModule, InputTextModule, SelectModule, TooltipModule, GridComponent],
  template: `
    <section class="feature-page">
      <header class="feature-hero">
        <div class="feature-title"><span class="feature-icon"><i class="pi pi-car"></i></span><div><h1>السيارات</h1><p>إدارة أسطول المصنع والسائقين وملكية المركبات.</p></div></div>
      </header>
      <app-grid title="سجل السيارات" exportName="cars" [columns]="columns" [source]="service"
        [rowActions]="actions" createPermission="cars.write" exportPermission="cars.export"
        (createClicked)="open()" />
    </section>
    <ng-template #actions let-row><div class="row-actions"><p-button icon="pi pi-eye" [rounded]="true" [text]="true" pTooltip="التفاصيل" (onClick)="details(row)"/><p-button icon="pi pi-pencil" [rounded]="true" [text]="true" pTooltip="تعديل" (onClick)="edit(row)"/></div></ng-template>
    <p-dialog [visible]="show()" (visibleChange)="show.set($event)" [modal]="true" [draggable]="false" [style]="{width:'min(560px, 94vw)'}" [header]="editing() ? 'تعديل السيارة' : 'إضافة سيارة'">
      <div class="form-grid">
        <div class="field span-2"><label>اسم السيارة</label><input pInputText [ngModel]="form().name" (ngModelChange)="patch('name',$event)" placeholder="مثال: سيارة نقل 1"/></div>
        <div class="field"><label>رقم اللوحة</label><input pInputText [ngModel]="form().plateNumber" (ngModelChange)="patch('plateNumber',$event)"/></div>
        <div class="field"><label>السائق</label><input pInputText [ngModel]="form().driverName" (ngModelChange)="patch('driverName',$event)"/></div>
        <div class="field span-2"><label>نوع الملكية</label><p-select [options]="typeOptions" optionLabel="1" optionValue="0" [ngModel]="form().type" (ngModelChange)="patch('type',+$event)"/></div>
      </div>
      <ng-template pTemplate="footer"><div class="dialog-actions"><p-button label="إلغاء" severity="secondary" [text]="true" (onClick)="show.set(false)"/><p-button label="حفظ" icon="pi pi-check" [loading]="saving()" [disabled]="!form().name.trim()" (onClick)="save()"/></div></ng-template>
    </p-dialog>`,
})
export class CarsComponent {
  readonly service = inject(CarsService);
  private readonly router = inject(Router);
  private readonly grid = viewChild.required(GridComponent);
  readonly show = signal(false);
  readonly saving = signal(false);
  readonly editing = signal<CarResponse | null>(null);
  readonly form = signal({ name: '', plateNumber: '', driverName: '', type: CarType.Owned });
  readonly typeOptions = optionsFrom(carTypeLabels);
  readonly columns: ColumnDef<CarResponse>[] = [
    { key: 'name', header: 'السيارة', type: GridFieldType.Text },
    { key: 'plateNumber', header: 'اللوحة', type: GridFieldType.Text },
    { key: 'driverName', header: 'السائق', type: GridFieldType.Text },
    { key: 'type', header: 'النوع', type: GridFieldType.Enum, options: this.typeOptions, format: row => carTypeLabels[row.type] },
  ];
  patch(key: string, value: unknown): void { this.form.update(form => ({ ...form, [key]: value })); }
  open(): void { this.editing.set(null); this.form.set({ name: '', plateNumber: '', driverName: '', type: CarType.Owned }); this.show.set(true); }
  edit(row: CarResponse): void { this.editing.set(row); this.form.set({ name: row.name, plateNumber: row.plateNumber ?? '', driverName: row.driverName ?? '', type: row.type }); this.show.set(true); }
  save(): void { this.saving.set(true); const row = this.editing(); (row ? this.service.update(row.id, this.form()) : this.service.create(this.form())).subscribe({ next: () => { this.show.set(false); this.grid().load(); }, error: () => this.saving.set(false), complete: () => this.saving.set(false) }); }
  details(row: CarResponse): void { this.router.navigate(['/cars', row.id]); }
}
