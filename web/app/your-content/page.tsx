import { cookies } from "next/headers";
import { AccessCodeForm } from "@/components/AccessCodeForm";
import { Container } from "@/components/Container";
import { PostList } from "@/components/PostList";
import { getYourContent } from "@/lib/api";
import { accessCookie } from "@/lib/access";
import { constructMetadata } from "@/lib/metadata";

// Per Client, so rendered per request and never cached.
export const dynamic = "force-dynamic";

export const metadata = constructMetadata({
  title: "Your content",
  path: "/your-content",
  noIndex: true,
});

export default async function YourContentPage() {
  const session = (await cookies()).get(accessCookie)?.value;
  const content = session ? await getYourContent(session) : null;

  return (
    <Container className="py-16 md:py-24">
      <h1 className="mb-4 text-4xl font-bold tracking-tight">Your content</h1>
      {content ? (
        <>
          <p className="text-muted-foreground mb-12 text-lg">
            Everything shared with {content.clientName}.
          </p>
          <PostList posts={content.posts} />
        </>
      ) : (
        <div className="max-w-xl">
          <p className="text-muted-foreground mb-8">
            Enter your Access Code to see what Stone has shared with you.
          </p>
          <AccessCodeForm />
        </div>
      )}
    </Container>
  );
}
