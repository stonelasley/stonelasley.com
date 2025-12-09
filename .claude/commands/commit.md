---
description: Generate a commit message based on staged changes and recent commit history.
---

The user may provide additional context as command arguments. Consider this context when generating the commit message. 
alphanumeric arguments are likely work item IDs

User context:

$ARGUMENTS

## Task

Confirm we are on a feature branch

1. **Analyze current branch**: Run `git status` to check current branch. 
2. [Optional]**Create Branch**: If we are on master branch create a feature branch with the naming convention 'st1/<type>/WorkItemId'
    - If no argument that looks like a workitem was provided ask for it. 

Generate a well-crafted commit message for the currently staged changes by following these steps:

1. **Analyze the changes**: Run `git diff --cached` to see all staged changes
2. **Understand the scope**: Determine which areas of the codebase are affected (e.g., specific modules, features, or layers)

## Commit Message Guidelines

Follow conventional commit format and adapt to the project's style:

- **Format**: `type(scope): brief description` or `type: brief description`
- **Common types**: `feat`, `fix`, `refactor`, `chore`, `docs`, `test`, `perf`, `style`, `ci`, `build`
- **Scope**: The module or area affected (optional but helpful)
- **Description**: Concise summary focusing on WHAT changed and WHY (not HOW)
- **Style**:
  - If introducing a breaking change e.g. a change to an existing Response Contract add '!' suffix to type e.g "feat(Handler)!"
  - Use imperative mood ("add" not "added" or "adds")
  - Keep first line under 72 characters when possible
  - Match the capitalization style of recent commits
  - Reference workitemIds/PR numbers if the project uses them

Format example: 
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
Refs: [optional ticket number] - Can be taken from last segment of branch name or argument

## Output

Generate 1 commit message and confirm with user before committing.

**Option 1 (Recommended):**
```
[commit message here]
```

**IMPORTANT**:
- Do NOT push EVER
- Do NOT commit to master EVER
- Do NOT actually create the commit until the user confirms
- If there are no staged changes, inform the user and suggest running `git add` first
- If the changes span multiple unrelated areas, suggest breaking them into separate commits
