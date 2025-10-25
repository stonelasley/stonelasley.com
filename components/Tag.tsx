import Link from "next/link";
import { cn } from "@/lib/utils";

interface TagProps {
  tag: string;
  active?: boolean;
  href?: string;
}

export function Tag({ tag, active = false, href }: TagProps) {
  const className = cn(
    "inline-flex items-center rounded-full px-3 py-1 text-xs font-medium transition-colors",
    active
      ? "bg-foreground text-background"
      : "bg-secondary text-secondary-foreground hover:bg-secondary/80"
  );

  if (href) {
    return (
      <Link href={href} className={className}>
        {tag}
      </Link>
    );
  }

  return <span className={className}>{tag}</span>;
}
