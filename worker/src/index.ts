export interface Env {
  DB: D1Database;
  WORKER_SECRET: string;
}

interface QueryRequest {
  sql: string;
  params?: unknown[];
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    if (!(await isAuthorized(request, env))) {
      return Response.json({ error: "unauthorized" }, { status: 401 });
    }

    const url = new URL(request.url);
    if (request.method === "POST" && url.pathname === "/d1/query") {
      return query(request, env);
    }

    return Response.json({ error: "not found" }, { status: 404 });
  },
} satisfies ExportedHandler<Env>;

async function query(request: Request, env: Env): Promise<Response> {
  let body: QueryRequest;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: "invalid json" }, { status: 400 });
  }
  if (typeof body.sql !== "string" || body.sql.length === 0) {
    return Response.json({ error: "sql is required" }, { status: 400 });
  }

  try {
    const { results } = await env.DB.prepare(body.sql)
      .bind(...(body.params ?? []))
      .all();
    return Response.json({ results });
  } catch (error) {
    return Response.json({ error: String(error) }, { status: 500 });
  }
}

async function isAuthorized(request: Request, env: Env): Promise<boolean> {
  const header = request.headers.get("Authorization") ?? "";
  if (!env.WORKER_SECRET || !header.startsWith("Bearer ")) {
    return false;
  }
  return timingSafeEqual(header.slice("Bearer ".length), env.WORKER_SECRET);
}

// Compares digests so the check takes the same time whatever the input length.
async function timingSafeEqual(a: string, b: string): Promise<boolean> {
  const encoder = new TextEncoder();
  const [da, db] = await Promise.all([
    crypto.subtle.digest("SHA-256", encoder.encode(a)),
    crypto.subtle.digest("SHA-256", encoder.encode(b)),
  ]);
  return crypto.subtle.timingSafeEqual(da, db);
}
