export const monthNames = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

/** A holiday year runs from 1 May to 30 April, like the old system and the usage period of week 6. */
export const HOLIDAY_YEAR_START_MONTH = 5;

export const isoDate = (year: number, month: number, day: number) =>
  `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;

export const daysInMonth = (year: number, month: number) => new Date(year, month, 0).getDate();

/** 0 = Monday ... 6 = Sunday */
export const weekdayIndex = (year: number, month: number, day: number) =>
  (new Date(year, month - 1, day).getDay() + 6) % 7;

export const isWeekend = (year: number, month: number, day: number) => weekdayIndex(year, month, day) >= 5;

/** May 2026 to April 2027 is the holiday year 2026. */
export const holidayYearOf = (year: number, month: number) =>
  month >= HOLIDAY_YEAR_START_MONTH ? year : year - 1;

export function shiftMonth(year: number, month: number, delta: number) {
  const index = year * 12 + (month - 1) + delta;
  return { year: Math.floor(index / 12), month: (index % 12) + 1 };
}

export function currentMonth() {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}
