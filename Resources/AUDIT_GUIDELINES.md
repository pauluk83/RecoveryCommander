# Audit-First Development Guidelines

To ensure perfect traceability and maintain a historical record of all changes, every source file in the RecoveryCommander project must adhere to the following audit standards.

---

## 📋 The Audit Header

Every source file must begin with a standardized audit block. This block identifies the file's purpose, creation details, and a comprehensive changelog.

### C# Standard Format
```csharp
/*
 * AUDIT HEADER
 * File: [Filename].cs
 * Module: [Feature/Module Name]
 * Created: [YYYY-MM-DD]
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * [YYYY-MM-DD] - [Version] - [Change Summary]
 */
```

### HTML/CSS/JS Standard Format
Use the appropriate comment syntax for the language, but maintain the same information structure.

---

## 🔄 Mirroring Process

1. **File-Level**: When a file is modified, a new entry must be added to the `CHANGELOG` section of its audit header.
2. **Project-Level**: Significant changes must be mirrored in the root `CHANGELOG.md` file.
3. **Traceability**: The summary in the file header should provide enough context to understand *why* the change was made, while the project changelog provides the *what* for the end-user.

---

## 🏗️ Build-Time Changelog Update Requirement

**CRITICAL**: Before every build, the root `CHANGELOG.md` must be updated with all ongoing changes.

### Pre-Build Checklist
1. **Review all modified files** since the last build
2. **Update file-level audit headers** with individual changelog entries
3. **Aggregate changes** into the root `CHANGELOG.md` with:
   - Date stamp (YYYY-MM-DD)
   - Version number (if applicable)
   - Categorized sections (UI, Security, Core, etc.)
   - Detailed change descriptions
4. **Verify completeness** - no changes should exist without corresponding changelog entries

### Build Enforcement
The build process will:
- Check for uncommitted changes
- Verify that `CHANGELOG.md` has been updated since the last commit
- Warn if significant file modifications lack corresponding changelog entries

---

## ⚖️ Integrity Rules

- **Never Delete**: Changelog entries are permanent. Do not remove old entries to save space.
- **Accuracy**: Ensure dates and version numbers match the official release tags.
- **Consistency**: Use the same terminology across both the file header and the project-wide changelog.
- **Completeness**: Every code change must have a corresponding audit trail entry.

---

## 📝 Example Workflow

### Making a Code Change
1. Modify the source file
2. Update the file's audit header with the change details
3. Update the root `CHANGELOG.md` with the aggregated change
4. Commit both changes together
5. Build and verify

### Example Entry
**File Header:**
```csharp
/*
 * CHANGELOG:
 * 2026-06-17 - 1.2.8 - Added async file download with progress reporting
 */
```

**Project CHANGELOG.md:**
```markdown
## 2026-06-17 - Build 1.2.8

### Core Features
- **Async Download System** — Added progress reporting for file downloads with cancellation support.
```

---

**RecoveryCommander** — *Traceable, Auditable, Reliable.*
