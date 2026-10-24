import type { PostSummary } from "@/lib/api";
import { PostCard } from "./PostCard";

interface PostListProps {
  posts: PostSummary[];
}

export function PostList({ posts }: PostListProps) {
  if (posts.length === 0) {
    return (
      <div className="py-12 text-center">
        <p className="text-muted-foreground">No posts yet.</p>
      </div>
    );
  }

  return (
    <div className="grid gap-8 md:gap-12">
      {posts.map((post) => (
        <PostCard key={post.id} post={post} />
      ))}
    </div>
  );
}
