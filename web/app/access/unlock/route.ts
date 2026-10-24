import { NextResponse } from "next/server";
import { exchangeAccessCode } from "@/lib/api";
import { accessCookie, accessCookieMaxAge } from "@/lib/access";

// The Access Code form posts here (it works without JavaScript, apart from Turnstile).
export async function POST(request: Request) {
  const form = await request.formData();
  const code = String(form.get("code") ?? "");
  // Turnstile's widget adds this field; without a site key (local, E2E) there's no widget and the API fakes it.
  const turnstile = String(form.get("cf-turnstile-response") ?? "local");
  const exchanged = await exchangeAccessCode(code, turnstile);
  const back = new URL("/access", request.url);
  if (!exchanged) {
    back.searchParams.set("error", "1");
    return NextResponse.redirect(back, 303);
  }

  // The page reads the Client's name from the session, so nothing from the URL is shown as theirs.
  back.searchParams.set("unlocked", "1");
  const response = NextResponse.redirect(back, 303);
  response.cookies.set(accessCookie, exchanged.session, {
    httpOnly: true,
    secure: new URL(request.url).protocol === "https:",
    sameSite: "lax",
    path: "/",
    maxAge: accessCookieMaxAge,
  });
  return response;
}
