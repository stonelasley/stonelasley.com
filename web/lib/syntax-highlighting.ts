import { codeToHtml } from "shiki";

const SUPPORTED_LANGUAGES = ["typescript", "javascript", "csharp", "rust", "lua"];

export async function highlightCode(html: string): Promise<string> {
  // Find all code blocks with language class
  const codeBlockRegex = /<pre><code class="language-(\w+)">([\s\S]*?)<\/code><\/pre>/g;

  let result = html;
  const matches = [...html.matchAll(codeBlockRegex)];

  for (const match of matches) {
    const [fullMatch, lang, code] = match;

    // Only highlight supported languages
    if (SUPPORTED_LANGUAGES.includes(lang.toLowerCase())) {
      try {
        // Decode HTML entities
        const decodedCode = code
          .replace(/&lt;/g, "<")
          .replace(/&gt;/g, ">")
          .replace(/&amp;/g, "&")
          .replace(/&quot;/g, '"')
          .replace(/&#39;/g, "'");

        const highlighted = await codeToHtml(decodedCode, {
          lang: lang.toLowerCase(),
          theme: "github-dark",
        });

        result = result.replace(fullMatch, highlighted);
      } catch (error) {
        console.error(`Failed to highlight code for language: ${lang}`, error);
        // Keep the original code block if highlighting fails
      }
    }
  }

  return result;
}
