import { cn } from "@/lib/utils";

interface ProseProps {
  children: React.ReactNode;
  className?: string;
}

export function Prose({ children, className }: ProseProps) {
  return (
    <div className={cn("prose prose-zinc dark:prose-invert", className)}>
      {children}
    </div>
  );
}
