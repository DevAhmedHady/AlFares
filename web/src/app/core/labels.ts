import { ActivityLevel, ChartType, ClientStatus, TodoPriority, TodoStatus } from './models';

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
