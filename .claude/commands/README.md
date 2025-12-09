# Spec-Kit Workflow Guide

## Overview

Spec-Kit is an open-source toolkit enabling **Spec-Driven Development** - a methodology where specifications become executable, directly generating working implementations rather than just guiding them. This approach emphasizes creating rich specifications through multi-step refinement before implementation.

## Core Philosophy

Traditional development has been code-first. Spec-Kit reverses this by making **specifications executable**, where AI agents interpret detailed specs to generate implementations. This supports three development phases:

- **Greenfield (0-to-1)**: Starting new projects from scratch
- **Creative Exploration**: Parallel implementations and experimentation
- **Iterative Enhancement**: Brownfield modernization and feature additions

## Installation

### Persistent Installation (Recommended)

```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
```

### One-Time Usage

```bash
uvx --from git+https://github.com/github/spec-kit.git specify init <PROJECT_NAME>
```

### Initialize a Project

```bash
# New project
specify init my-project --ai claude

# Initialize in current directory
specify init . --here --ai claude

# Force merge into non-empty directory
specify init . --force --ai claude

# PowerShell variant
specify init my-project --ai claude --script ps
```

### Check Installed Tools

```bash
specify check
```

## Directory Structure

After initialization, Spec-Kit creates the `.specify/` directory:

```
.specify/
├── memory/
│   └── constitution.md          # Project principles & governance
├── specs/
│   └── [###-feature-name]/
│       ├── spec.md              # Functional requirements & user stories
│       ├── plan.md              # Technical implementation plan
│       ├── tasks.md             # Task breakdown with dependencies
│       ├── research.md          # Technology stack research
│       ├── data-model.md        # Data structure specifications
│       ├── quickstart.md        # Setup & execution guide
│       └── contracts/           # API specs, contracts
│           ├── api-spec.json
│           └── endpoint-spec.md
└── templates/
    ├── spec-template.md
    ├── plan-template.md
    ├── tasks-template.md
    ├── checklist-template.md
    └── agent-file-template.md
```

## Workflow Phases

### Phase 1: Constitution (Foundation)

**Command**: `/speckit.constitution`

**Purpose**: Establish governing principles and development guidelines that direct all subsequent work.

**When to Use**:
- At project start (one-time setup)
- When updating project governance
- When adding/modifying core principles

**What It Creates**:
- `.specify/memory/constitution.md`

**Focus Areas**:
- Code quality standards
- Testing expectations and methodologies
- User experience consistency rules
- Performance benchmarks and targets
- Architectural constraints
- Technology stack decisions
- Governance and amendment process

**Example Usage**:
```
/speckit.constitution
```

**Output**: A versioned constitution document with core principles, architecture constraints, quality standards, and governance rules.

---

### Phase 2: Specification (Requirements)

**Command**: `/speckit.specify [feature description]`

**Purpose**: Define functional requirements and user stories focusing on *what* you want to build and *why*, not *how* to build it.

**When to Use**:
- Starting a new feature
- Defining user requirements
- Before any technical planning

**What It Creates**:
- `.specify/specs/[###-feature-name]/spec.md`

**Focus Areas**:
- User scenarios and journeys (prioritized as P1, P2, P3)
- Acceptance criteria (Given/When/Then format)
- Functional requirements (FR-001, FR-002, etc.)
- Key entities (data models, conceptual)
- Success criteria (measurable outcomes)
- Edge cases and error scenarios
- Constitution compliance checklist

**Best Practices**:
- Avoid mentioning specific technologies
- Focus on user value and business outcomes
- Use plain language, not technical jargon
- Prioritize user stories by importance
- Make each story independently testable
- Define clear, measurable acceptance criteria

**Example Usage**:
```
/speckit.specify Add dark mode toggle to the application with user preference persistence
```

**Output**: A specification document with prioritized user stories, functional requirements, and success criteria.

---

### Phase 3: Clarification (Validation)

**Command**: `/speckit.clarify`

**Purpose**: Identify underspecified areas in the specification by asking targeted clarification questions and encoding answers back into the spec.

**When to Use**:
- After creating initial specification
- Before technical planning
- When ambiguities or gaps are detected

**What It Does**:
- Analyzes `spec.md` for unclear requirements
- Generates up to 5 targeted questions
- Updates `spec.md` with answers
- Ensures specification completeness

**Focus Areas**:
- Ambiguous requirements
- Missing edge cases
- Unclear acceptance criteria
- Incomplete user scenarios
- Undefined constraints

**Best Practices**:
- Run before `/speckit.plan` to avoid rework
- Answer questions thoroughly
- Request re-clarification if needed
- Verify all "NEEDS CLARIFICATION" markers are resolved

**Example Usage**:
```
/speckit.clarify
```

**Output**: Updated `spec.md` with resolved ambiguities and clearer requirements.

---

### Phase 4: Planning (Technical Design)

**Command**: `/speckit.plan`

**Purpose**: Create technical implementation plans with your chosen tech stack, architecture, and constraints.

**When to Use**:
- After specification is complete and clarified
- When ready to define technical approach
- Before breaking down into tasks

**What It Creates**:
- `.specify/specs/[###-feature-name]/plan.md`
- `.specify/specs/[###-feature-name]/research.md` (technology research)
- `.specify/specs/[###-feature-name]/data-model.md` (data structures)
- `.specify/specs/[###-feature-name]/quickstart.md` (setup guide)
- `.specify/specs/[###-feature-name]/contracts/` (API specs)

**Focus Areas**:
- Technology stack selection
- Architecture and design patterns
- Project structure and file organization
- Data models and schemas
- API contracts and interfaces
- Constitution compliance checks
- Complexity justification

**Best Practices**:
- Reference the constitution for constraints
- Justify any principle violations
- Document simpler alternatives rejected
- Define clear project structure
- Create detailed API contracts
- Research technology choices thoroughly

**Example Usage**:
```
/speckit.plan
```

**Output**: Comprehensive technical plan with architecture decisions, data models, API contracts, and implementation strategy.

---

### Phase 5: Task Breakdown

**Command**: `/speckit.tasks`

**Purpose**: Generate actionable, dependency-ordered task lists for implementation based on the plan and spec.

**When to Use**:
- After planning is complete
- Before implementation begins
- When you need a clear execution roadmap

**What It Creates**:
- `.specify/specs/[###-feature-name]/tasks.md`

**Task Organization**:
- **Phase 1: Setup** - Project initialization, tooling
- **Phase 2: Foundational** - Core infrastructure blocking all features
- **Phase 3+: User Stories** - One phase per user story (P1, P2, P3)
- **Final Phase: Polish** - Cross-cutting concerns, quality checks

**Task Format**: `[ID] [P?] [Story] Description`
- `[P]` - Can run in parallel (different files, no dependencies)
- `[Story]` - User story association (US1, US2, US3)
- Includes exact file paths

**Best Practices**:
- Tasks grouped by user story for independent implementation
- Foundational phase must complete before any user story
- Each user story independently completable and testable
- Tests written first (if included), must fail before implementation
- Clear dependency chains documented
- Parallel opportunities identified with [P] markers

**Example Usage**:
```
/speckit.tasks
```

**Output**: Ordered task list with dependencies, parallel opportunities, and implementation strategy.

---

### Phase 6: Implementation

**Command**: `/speckit.implement`

**Purpose**: Execute all tasks systematically to build the feature according to the plan.

**When to Use**:
- After tasks are generated and reviewed
- When ready to implement the feature
- For systematic, guided implementation

**What It Does**:
- Processes each task in `tasks.md` sequentially
- Respects dependencies and parallel opportunities
- Creates code, tests, documentation
- Verifies completion at each checkpoint
- Handles errors and blockers

**Implementation Strategy**:
- **MVP First**: Complete Setup → Foundational → User Story 1 (P1) → Validate
- **Incremental Delivery**: Add one user story at a time, test independently
- **Parallel Team**: Multiple developers work on different user stories simultaneously

**Best Practices**:
- Stop at checkpoints to validate independently
- Run tests before and after implementation
- Commit after each task or logical group
- Address errors immediately
- Follow constitution compliance checks in final phase

**Example Usage**:
```
/speckit.implement
```

**Output**: Fully implemented feature matching specification and plan.

---

## Optional Quality Commands

### `/speckit.analyze`

**Purpose**: Perform cross-artifact consistency and quality analysis across `spec.md`, `plan.md`, and `tasks.md`.

**When to Use**:
- After task generation
- Before implementation
- For quality assurance

**What It Checks**:
- Consistency between spec and plan
- Coverage of all user stories in tasks
- Constitution compliance
- Missing requirements or acceptance criteria
- Task dependency correctness

---

### `/speckit.checklist`

**Purpose**: Generate custom quality checklists validating requirements completeness.

**When to Use**:
- For manual quality gates
- Pre-deployment verification
- Testing guidance
- Code review checklists

**What It Creates**:
- Custom checklist files based on feature context
- Category-organized verification items
- Numbered items for easy reference

---

### `/speckit.taskstoissues`

**Purpose**: Convert existing tasks into GitHub issues with dependencies and labels.

**When to Use**:
- When using GitHub for project management
- For team collaboration on tasks
- To track progress in GitHub Projects

---

## Recommended Workflow

### Complete Flow (Greenfield)

1. **Initialize Project**
   ```bash
   specify init my-project --ai claude
   ```

2. **Establish Governance**
   ```
   /speckit.constitution
   ```

3. **Define Requirements**
   ```
   /speckit.specify [feature description]
   ```

4. **Clarify Ambiguities**
   ```
   /speckit.clarify
   ```

5. **Create Technical Plan**
   ```
   /speckit.plan
   ```

6. **Generate Tasks**
   ```
   /speckit.tasks
   ```

7. **Quality Check** (Optional)
   ```
   /speckit.analyze
   ```

8. **Implement Feature**
   ```
   /speckit.implement
   ```

### Quick Flow (Brownfield/Existing Project)

1. **Add Feature Spec**
   ```
   /speckit.specify [feature description]
   ```

2. **Plan Implementation**
   ```
   /speckit.plan
   ```

3. **Generate Tasks**
   ```
   /speckit.tasks
   ```

4. **Implement**
   ```
   /speckit.implement
   ```

## Environment Variables

**`SPECIFY_FEATURE`**: Override feature detection in non-Git repositories. Set to feature directory name when working outside Git branches.

```bash
export SPECIFY_FEATURE="001-dark-mode-toggle"
```

## Key Principles

### 1. Specification First
Define *what* and *why* before *how*. Rich specifications enable better AI interpretation.

### 2. Multi-Step Refinement
Iterate through constitution → specify → clarify → plan → tasks → implement.

### 3. Constitution Governance
All work must align with established principles and constraints.

### 4. Independent User Stories
Each user story should be independently implementable, testable, and deployable.

### 5. Dependency Management
Clear task dependencies enable parallel execution and incremental delivery.

### 6. Quality Gates
Constitution compliance checks at spec, plan, and implementation phases.

## Supported AI Agents

- Claude Code (Claude)
- GitHub Copilot
- Cursor
- Windsurf
- Gemini CLI
- Amazon Q
- And 10+ additional platforms

## Tips & Best Practices

### Specification Phase
- Use plain language, avoid technical terms
- Focus on user value and outcomes
- Prioritize user stories (P1, P2, P3)
- Make acceptance criteria specific and testable
- Include edge cases and error scenarios

### Planning Phase
- Reference constitution for constraints
- Justify complexity and principle violations
- Document alternatives considered
- Create detailed API contracts
- Define clear data models

### Task Phase
- Group tasks by user story for independence
- Identify parallel opportunities with [P]
- Include exact file paths in task descriptions
- Document dependencies clearly
- Plan for constitution compliance checks in final phase

### Implementation Phase
- Stop at checkpoints to validate
- Test independently before moving forward
- Commit frequently (per task or logical group)
- Address errors immediately, don't skip
- Run all quality checks before completion

## Common Issues & Solutions

### "Feature detection failed"
- Ensure you're on a feature branch matching `###-feature-name` pattern
- Or set `SPECIFY_FEATURE` environment variable

### "Constitution not found"
- Run `/speckit.constitution` first
- Verify `.specify/memory/constitution.md` exists

### "Spec missing required sections"
- Run `/speckit.clarify` to identify gaps
- Ensure all user stories have acceptance criteria

### "Tasks don't match plan"
- Regenerate tasks with `/speckit.tasks` after plan updates
- Run `/speckit.analyze` to check consistency

## Additional Resources

- **Official Repo**: https://github.com/github/spec-kit
- **Constitution Template**: `.specify/memory/constitution.md`
- **Spec Template**: `.specify/templates/spec-template.md`
- **Plan Template**: `.specify/templates/plan-template.md`
- **Tasks Template**: `.specify/templates/tasks-template.md`

---

**Last Updated**: 2025-12-04
**Spec-Kit Version**: Latest (GitHub main branch)
**Project**: stonelasley.com
