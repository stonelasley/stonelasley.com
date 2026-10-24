export interface Env {
  DB: D1Database;
  IMAGES: R2Bucket;
  VECTORS: VectorizeIndex;
  AI: Ai;
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
    if (request.method === "POST" && url.pathname === "/ai/embed") {
      return embed(request, env);
    }
    if (request.method === "POST" && url.pathname.startsWith("/vectors/")) {
      return vectors(request, url.pathname.slice("/vectors/".length), env);
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

// The vector leg's model; changing it changes every vector, so it's part of the index version (#44).
const EMBEDDING_MODEL = "@cf/baai/bge-base-en-v1.5";

async function embed(request: Request, env: Env): Promise<Response> {
  const body = await readJson<{ texts?: unknown }>(request);
  if (!body || !Array.isArray(body.texts) || body.texts.some((t) => typeof t !== "string")) {
    return Response.json({ error: "texts must be an array of strings" }, { status: 400 });
  }
  try {
    const result = await env.AI.run(EMBEDDING_MODEL, { text: body.texts as string[] });
    return Response.json({ vectors: (result as { data: number[][] }).data });
  } catch (error) {
    return Response.json({ error: String(error) }, { status: 500 });
  }
}

// POST /vectors/upsert { vectors }, /vectors/query { vector, topK, filter?, namespace? },
// /vectors/delete { ids }, /vectors/get { ids }: thin passthroughs to the Vectorize binding.
async function vectors(request: Request, operation: string, env: Env): Promise<Response> {
  const body = await readJson<Record<string, unknown>>(request);
  if (!body) {
    return Response.json({ error: "invalid json" }, { status: 400 });
  }
  try {
    switch (operation) {
      case "upsert":
        return Response.json(await env.VECTORS.upsert(body.vectors as VectorizeVector[]));
      case "query":
        return Response.json(
          await env.VECTORS.query(body.vector as number[], {
            topK: body.topK as number,
            filter: body.filter as VectorizeVectorMetadataFilter | undefined,
            namespace: body.namespace as string | undefined,
            returnMetadata: "indexed",
          })
        );
      case "delete":
        return Response.json(await env.VECTORS.deleteByIds(body.ids as string[]));
      case "get":
        return Response.json(await env.VECTORS.getByIds(body.ids as string[]));
      default:
        return Response.json({ error: "not found" }, { status: 404 });
    }
  } catch (error) {
    return Response.json({ error: String(error) }, { status: 500 });
  }
}

async function readJson<T>(request: Request): Promise<T | null> {
  try {
    return (await request.json()) as T;
  } catch {
    return null;
  }
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
