import { Component, inject } from '@angular/core';
import { GridComponent } from '../../shared/grid/grid';
import { ColumnDef } from '../../shared/grid/grid-column';
import { UsersService } from '../../core/api/resources';
import { GridFieldType } from '../../core/grid.models';
import { UserResponse } from '../../core/models';
import { formatDate } from '../../core/labels';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [GridComponent],
  template: `<app-grid title="المستخدمون" [columns]="columns" [source]="service" />`,
})
export class UsersComponent {
  readonly service = inject(UsersService);

  readonly columns: ColumnDef<UserResponse>[] = [
    { key: 'email', header: 'البريد الإلكتروني', type: GridFieldType.Text },
    { key: 'displayName', header: 'الاسم', type: GridFieldType.Text },
    { key: 'isActive', header: 'نشط', type: GridFieldType.Boolean, options: [['true', 'نعم'], ['false', 'لا']], format: (r) => (r.isActive ? 'نعم' : 'لا') },
    { key: 'createdAt', header: 'تاريخ الإنشاء', type: GridFieldType.Date, filterable: false, format: (r) => formatDate(r.createdAtUtc) },
  ];
}
