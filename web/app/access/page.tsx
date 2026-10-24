import Script from "next/script";
import { cookies } from "next/headers";
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
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;

  return (
    <Container className="max-w-xl py-16 md:py-24">
      <h1 className="mb-4 text-4xl font-bold tracking-tight">Access</h1>
      <p className="text-muted-foreground mb-8">
        Coaching Clients: enter the Access Code Stone gave you. You only need to do this once on
        each device.
      </p>

      {clientName && (
        <p role="status" className="border-border bg-card mb-6 rounded-md border p-4">
          Unlocked for {clientName}.
        </p>
      )}
      {error && (
        <p role="alert" className="border-destructive text-destructive mb-6 rounded-md border p-4">
          That code didn&apos;t work.
        </p>
      )}

      <form action="/access/unlock" method="post" className="space-y-4">
        <div className="space-y-2">
          <label htmlFor="code" className="text-sm font-medium">
            Access Code
          </label>
          <input
            id="code"
            name="code"
            required
            autoComplete="off"
            autoCapitalize="characters"
            spellCheck={false}
            className="border-border bg-background focus:ring-ring w-full rounded-md border px-3 py-2 font-mono tracking-wider focus:ring-2 focus:outline-none"
          />
        </div>
        {siteKey && (
          <>
            <Script
              src="https://challenges.cloudflare.com/turnstile/v0/api.js"
              strategy="afterInteractive"
            />
            <div className="cf-turnstile" data-sitekey={siteKey} />
          </>
        )}
        <button
          type="submit"
          className="bg-foreground text-background hover:bg-foreground/90 rounded-md px-4 py-2 text-sm font-semibold"
        >
          Unlock
        </button>
      </form>
    </Container>
  );
}
