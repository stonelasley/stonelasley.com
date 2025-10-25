import { marked } from "marked";

/**
 * Configure marked options
 */
marked.setOptions({
  gfm: true, // GitHub Flavored Markdown
  breaks: true, // Convert \n to <br>
});

/**
 * Convert Markdown to HTML
 */
export async function markdownToHtml(markdown: string): Promise<string> {
  if (!markdown) return "";

  try {
    const html = await marked.parse(markdown);
    return html;
  } catch (error) {
    console.error("Error converting markdown to HTML:", error);
    return markdown; // Return original if conversion fails
  }
}
