import { type ClassValue, clsx } from "clsx";
import { format, formatDistance } from "date-fns";

export function cn(...inputs: ClassValue[]) {
  return clsx(inputs);
}

export function formatDate(date: string | Date, formatStr: string = "MMMM d, yyyy"): string {
  return format(new Date(date), formatStr);
}

export function formatRelativeDate(date: string | Date): string {
  return formatDistance(new Date(date), new Date(), { addSuffix: true });
}

export function absoluteUrl(path: string, baseUrl?: string): string {
  const base = baseUrl || process.env.NEXT_PUBLIC_SITE_URL || "https://stonelasley.com";
  return `${base}${path}`;
}

export function getBaseUrl(): string {
  if (typeof window !== "undefined") return "";
  if (process.env.VERCEL_URL) return `https://${process.env.VERCEL_URL}`;
  if (process.env.NEXT_PUBLIC_SITE_URL) return process.env.NEXT_PUBLIC_SITE_URL;
  return "http://localhost:3000";
}
