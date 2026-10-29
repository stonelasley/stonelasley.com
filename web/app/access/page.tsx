import Link from "next/link";
import { cookies } from "next/headers";
import { AccessCodeForm } from "@/components/AccessCodeForm";
import { Container } from "@/components/Container";
import { getAccessClientName } from "@/lib/api";
import { accessCookie } from "@/lib/access";
import { constructMetadata } from "@/lib/metadata";

export const metadata = constructMetadata({ title: "Access", path: "/access", noIndex: true });

interface AccessPageProps {
  searchParams: Promise<{ error?: string; unlocked?: string }>; // unlocked=1 after a successful exchange
}

export const dynamic = "force-dynamic";

export default async function AccessPage({ searchParams }: AccessPageProps) {
  const { error, unlocked } = await searchParams;
  const session = (await cookies()).get(accessCookie)?.value;
  const clientName = unlocked && session ? await getAccessClientName(session) : null;

  return (
    <Container className="max-w-xl py-16 md:py-24">
      <h1 className="text-title mb-4 font-bold tracking-tight">Access</h1>
      <p className="text-muted-foreground mb-8">
        Coaching Clients: enter the Access Code Stone gave you. You only need to do this once on
        each device.
      </p>

      {clientName && (
        <p role="status" className="border-border bg-card mb-6 rounded-md border p-4">
          Unlocked for {clientName}.{" "}
          <Link href="/your-content" className="underline">
            See your content
          </Link>
        </p>
      )}
      {error && (
        <p role="alert" className="border-destructive text-destructive mb-6 rounded-md border p-4">
          That code didn&apos;t work.
        </p>
      )}

      <AccessCodeForm />
    </Container>
  );
}
