import { GridFilter, GridFilterOp } from './grid.models';

/** Arabic month labels (1–12) for scope filter dropdowns. */
export const monthOptions: { label: string; value: number }[] = [
  { label: 'يناير', value: 1 },
  { label: 'فبراير', value: 2 },
  { label: 'مارس', value: 3 },
  { label: 'أبريل', value: 4 },
  { label: 'مايو', value: 5 },
  { label: 'يونيو', value: 6 },
  { label: 'يوليو', value: 7 },
  { label: 'أغسطس', value: 8 },
  { label: 'سبتمبر', value: 9 },
  { label: 'أكتوبر', value: 10 },
  { label: 'نوفمبر', value: 11 },
  { label: 'ديسمبر', value: 12 },
];

/** Year dropdown options from (current − span) through current year. */
export function yearOptions(span = 15): { label: string; value: number }[] {
  const current = new Date().getFullYear();
  const years: { label: string; value: number }[] = [];
  for (let y = current; y >= current - span; y--) {
    years.push({ label: String(y), value: y });
  }
  return years;
}

/** Days 1–N for the given year/month (defaults to 31 when incomplete). */
export function dayOptions(year: number | null, month: number | null): { label: string; value: number }[] {
  const max = year && month ? new Date(year, month, 0).getDate() : 31;
  return Array.from({ length: max }, (_, i) => ({ label: String(i + 1), value: i + 1 }));
}

/** Validates Year/Month/Day hierarchy; returns an Arabic error or null. */
export function validateYmd(year: number | null, month: number | null, day: number | null): string | null {
  if ((month != null || day != null) && year == null) {
    return 'اختر السنة أولاً قبل الشهر أو اليوم.';
  }
  if (day != null && month == null) {
    return 'اختر الشهر أولاً قبل اليوم.';
  }
  if (year != null && month != null && day != null) {
    const max = new Date(year, month, 0).getDate();
    if (day < 1 || day > max) return 'اليوم غير صالح لهذا الشهر.';
  }
  return null;
}

function pad(n: number): string {
  return String(n).padStart(2, '0');
}

/** Maps Year/Month/Day selections to `date` grid filters (Gte/Lte or Eq). */
export function ymdDateFilters(
  year: number | null,
  month: number | null,
  day: number | null,
): GridFilter[] {
  if (year == null) return [];
  if (month != null && day != null) {
    const iso = `${year}-${pad(month)}-${pad(day)}`;
    return [{ field: 'date', op: GridFilterOp.Eq, value: iso }];
  }
  if (month != null) {
    const last = new Date(year, month, 0).getDate();
    return [
      { field: 'date', op: GridFilterOp.Gte, value: `${year}-${pad(month)}-01` },
      { field: 'date', op: GridFilterOp.Lte, value: `${year}-${pad(month)}-${pad(last)}` },
    ];
  }
  return [
    { field: 'date', op: GridFilterOp.Gte, value: `${year}-01-01` },
    { field: 'date', op: GridFilterOp.Lte, value: `${year}-12-31` },
  ];
}
