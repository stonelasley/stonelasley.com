export interface Env {
  DB: D1Database;
  IMAGES: R2Bucket;
  WORKER_SECRET: string;
  // Local only: serve images publicly at /public/images/*, standing in for the R2 custom domain.
  SERVE_IMAGES?: string;
}

interface QueryRequest {
  sql: string;
  params?: unknown[];
}

interface BatchRequest {
  statements: QueryRequest[];
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (
      env.SERVE_IMAGES === "true" &&
      request.method === "GET" &&
      url.pathname.startsWith("/public/images/")
    ) {
      return publicImage(url.pathname.slice("/public/".length), env);
    }

    if (!(await isAuthorized(request, env))) {
      return Response.json({ error: "unauthorized" }, { status: 401 });
    }

    if (request.method === "POST" && url.pathname === "/d1/query") {
      return query(request, env);
    }
    if (request.method === "POST" && url.pathname === "/d1/batch") {
      return batch(request, env);
    }
    if (url.pathname.startsWith("/r2/objects/")) {
      return object(request, url.pathname.slice("/r2/objects/".length), env);
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

// D1 runs a batch as one transaction: every statement applies, or none does.
async function batch(request: Request, env: Env): Promise<Response> {
  let body: BatchRequest;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: "invalid json" }, { status: 400 });
  }
  if (
    !Array.isArray(body.statements) ||
    body.statements.length === 0 ||
    body.statements.some((s) => typeof s?.sql !== "string" || s.sql.length === 0)
  ) {
    return Response.json(
      { error: "statements must be a non-empty array of { sql, params? }" },
      { status: 400 }
    );
  }

  try {
    const results = await env.DB.batch(
      body.statements.map((s) => env.DB.prepare(s.sql).bind(...(s.params ?? [])))
    );
    return Response.json({ results: results.map((r) => r.results) });
  } catch (error) {
    return Response.json({ error: String(error) }, { status: 500 });
  }
}

// HEAD tells the API whether an image is already stored; PUT stores one. Keys are content hashes, so an
// object never changes once written.
async function object(request: Request, key: string, env: Env): Promise<Response> {
  if (!key) {
    return Response.json({ error: "key is required" }, { status: 400 });
  }
  if (request.method === "HEAD") {
    const head = await env.IMAGES.head(key);
    return head
      ? new Response(null, { headers: { "x-uploaded": head.uploaded.toISOString() } })
      : new Response(null, { status: 404 });
  }
  if (request.method === "PUT") {
    const contentType = request.headers.get("content-type") ?? "application/octet-stream";
    await env.IMAGES.put(key, request.body, {
      httpMetadata: { contentType, cacheControl: "public, max-age=31536000, immutable" },
    });
    return new Response(null, { status: 201 });
  }
  return Response.json({ error: "method not allowed" }, { status: 405 });
}

async function publicImage(key: string, env: Env): Promise<Response> {
  const image = await env.IMAGES.get(key);
  if (!image) {
    return new Response(null, { status: 404 });
  }
  const headers = new Headers();
  image.writeHttpMetadata(headers);
  return new Response(image.body, { headers });
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
