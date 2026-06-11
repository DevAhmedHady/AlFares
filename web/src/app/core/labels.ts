import { ActivityLevel, CarType, ChartType, ClientStatus, ExpenseScope, OwnerType, TodoPriority, TodoStatus } from './models';

export const clientStatusLabels: Record<number, string> = { [ClientStatus.Active]: 'نشط', [ClientStatus.Inactive]: 'غير نشط' };
export const activityLabels: Record<number, string> = {
  [ActivityLevel.Low]: 'منخفض', [ActivityLevel.Medium]: 'متوسط', [ActivityLevel.High]: 'مرتفع',
};
export const todoStatusLabels: Record<number, string> = {
  [TodoStatus.Open]: 'مفتوحة', [TodoStatus.InProgress]: 'قيد التنفيذ', [TodoStatus.Done]: 'مكتملة',
};
export const todoPriorityLabels: Record<number, string> = {
  [TodoPriority.Low]: 'منخفضة', [TodoPriority.Normal]: 'عادية', [TodoPriority.High]: 'عالية', [TodoPriority.Urgent]: 'عاجلة',
};
export const chartTypeLabels: Record<number, string> = {
  [ChartType.Bar]: 'أعمدة', [ChartType.Pie]: 'دائري', [ChartType.Line]: 'خطي',
};
export const carTypeLabels:Record<number,string>={[CarType.Owned]:'مملوكة',[CarType.Rented]:'مؤجرة'};
export const expenseScopeLabels:Record<number,string>={[ExpenseScope.General]:'عام',[ExpenseScope.Car]:'سيارة'};
export const ownerTypeLabels:Record<number,string>={[OwnerType.General]:'عام',[OwnerType.Client]:'عميل',[OwnerType.OwnedCar]:'سيارة مملوكة',[OwnerType.RentedCar]:'سيارة مؤجرة',[OwnerType.Worker]:'عامل'};

export function optionsFrom(labels: Record<number, string>): [string, string][] {
  return Object.entries(labels).map(([k, v]) => [k, v]);
}

export function formatDate(iso?: string | null): string {
  if (!iso) return '';
  return new Date(iso).toLocaleDateString('ar-EG', { year: 'numeric', month: '2-digit', day: '2-digit' });
}

export function formatMoney(value: number): string {
  return new Intl.NumberFormat('ar-EG', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
}

/** Bridges a 'YYYY-MM-DD' string (the wire/form format) to the Date that p-datepicker binds. */
export function toDate(iso?: string | null): Date | null {
  if (!iso) return null;
  const d = new Date(`${iso}T00:00:00`);
  return Number.isNaN(d.getTime()) ? null : d;
}

/** Converts a p-datepicker Date back to the 'YYYY-MM-DD' string used everywhere else. */
export function toIso(d?: Date | null): string {
  if (!d) return '';
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}
