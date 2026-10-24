import { describe, expect, it, vi } from "vitest";
import { warmApi } from "./warm-api.mjs";

const noDelay = () => Promise.resolve();

describe("warmApi", () => {
  it("resolves once /health answers OK", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response("Healthy", { status: 200 }));

    await warmApi({ url: "http://api.test", fetch, sleep: noDelay });

    expect(fetch).toHaveBeenCalledOnce();
    expect(fetch.mock.calls[0][0]).toBe("http://api.test/health");
  });

  it("retries while the API is starting", async () => {
    const fetch = vi
      .fn()
      .mockRejectedValueOnce(new TypeError("fetch failed"))
      .mockResolvedValueOnce(new Response("Unhealthy", { status: 503 }))
      .mockResolvedValueOnce(new Response("Healthy", { status: 200 }));

    await warmApi({ url: "http://api.test", fetch, sleep: noDelay, attempts: 3 });

    expect(fetch).toHaveBeenCalledTimes(3);
  });

  it("fails after the last attempt", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response("Unhealthy", { status: 503 }));

    await expect(
      warmApi({ url: "http://api.test", fetch, sleep: noDelay, attempts: 2 })
    ).rejects.toThrow("http://api.test/health did not become healthy after 2 attempts");
    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it("gives each attempt a timeout", async () => {
    const fetch = vi.fn().mockResolvedValue(new Response("Healthy", { status: 200 }));

    await warmApi({ url: "http://api.test", fetch, sleep: noDelay, timeoutMs: 1234 });

    expect(fetch.mock.calls[0][1].signal).toBeInstanceOf(AbortSignal);
  });
});
