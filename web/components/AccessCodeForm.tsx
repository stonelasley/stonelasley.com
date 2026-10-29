import Script from "next/script";

/** The Access Code form. Posts to /access/unlock; the Turnstile widget appears when a site key is set. */
export function AccessCodeForm() {
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  return (
    <form action="/access/unlock" method="post" className="space-y-4">
      <div className="space-y-2">
        <label htmlFor="code" className="text-small font-medium">
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
        className="bg-foreground text-background hover:bg-foreground/90 text-small rounded-md px-4 py-2 font-semibold"
      >
        Unlock
      </button>
    </form>
  );
}
