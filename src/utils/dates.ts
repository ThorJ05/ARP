/** Today's local date as yyyy-MM-dd (the format date inputs use). */
export const today = () => new Date().toLocaleDateString("sv-SE");

/**
 * Shows a date from the API the same way everywhere in the app: "2027-01-11" becomes "11-01-2027".
 * Also works for timestamps like "2027-01-11T09:30:00Z" (only the date part is shown).
 */
export function formatDate(value?: string | null): string {
  if (!value) return "";
  const [year, month, day] = value.slice(0, 10).split("-");
  return `${day}-${month}-${year}`;
}
