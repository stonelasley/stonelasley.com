import type { PostSummary } from "@/lib/api";
import { PostTile } from "./PostTile";

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
    <div className="grid gap-x-7 gap-y-10 sm:grid-cols-2 lg:grid-cols-3">
      {posts.map((post) => (
        <PostTile key={post.id} post={post} />
      ))}
    </div>
  );
}
