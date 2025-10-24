import Link from "next/link";
import { cn } from "@/lib/utils";

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  basePath: string;
  searchParams?: Record<string, string>;
}

export function Pagination({
  currentPage,
  totalPages,
  basePath,
  searchParams = {},
}: PaginationProps) {
  if (totalPages <= 1) return null;

  const createPageUrl = (page: number) => {
    const params = new URLSearchParams(searchParams);
    if (page > 1) {
      params.set("page", page.toString());
    } else {
      params.delete("page");
    }
    const queryString = params.toString();
    return `${basePath}${queryString ? `?${queryString}` : ""}`;
  };

  const pages = [];
  const showPages = 5; // Show 5 page numbers
  let startPage = Math.max(1, currentPage - Math.floor(showPages / 2));
  let endPage = Math.min(totalPages, startPage + showPages - 1);

  if (endPage - startPage + 1 < showPages) {
    startPage = Math.max(1, endPage - showPages + 1);
  }

  for (let i = startPage; i <= endPage; i++) {
    pages.push(i);
  }

  return (
    <nav
      aria-label="Pagination"
      className="flex items-center justify-center gap-2 mt-12"
    >
      {currentPage > 1 && (
        <Link
          href={createPageUrl(currentPage - 1)}
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-4 py-2 text-sm font-medium hover:bg-accent transition-colors"
          aria-label="Go to previous page"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            className="h-4 w-4"
          >
            <path
              fillRule="evenodd"
              d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z"
              clipRule="evenodd"
            />
          </svg>
          Previous
        </Link>
      )}
      {startPage > 1 && (
        <>
          <Link
            href={createPageUrl(1)}
            className={cn(
              "inline-flex h-10 w-10 items-center justify-center rounded-md border border-border text-sm font-medium transition-colors",
              "hover:bg-accent"
            )}
          >
            1
          </Link>
          {startPage > 2 && <span className="px-2">...</span>}
        </>
      )}
      {pages.map((page) => (
        <Link
          key={page}
          href={createPageUrl(page)}
          aria-current={page === currentPage ? "page" : undefined}
          className={cn(
            "inline-flex h-10 w-10 items-center justify-center rounded-md border text-sm font-medium transition-colors",
            page === currentPage
              ? "border-transparent bg-foreground text-background"
              : "border-border hover:bg-accent"
          )}
        >
          {page}
        </Link>
      ))}
      {endPage < totalPages && (
        <>
          {endPage < totalPages - 1 && <span className="px-2">...</span>}
          <Link
            href={createPageUrl(totalPages)}
            className={cn(
              "inline-flex h-10 w-10 items-center justify-center rounded-md border border-border text-sm font-medium transition-colors",
              "hover:bg-accent"
            )}
          >
            {totalPages}
          </Link>
        </>
      )}
      {currentPage < totalPages && (
        <Link
          href={createPageUrl(currentPage + 1)}
          className="inline-flex items-center gap-2 rounded-md border border-border bg-background px-4 py-2 text-sm font-medium hover:bg-accent transition-colors"
          aria-label="Go to next page"
        >
          Next
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            className="h-4 w-4"
          >
            <path
              fillRule="evenodd"
              d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
              clipRule="evenodd"
            />
          </svg>
        </Link>
      )}
    </nav>
  );
}
