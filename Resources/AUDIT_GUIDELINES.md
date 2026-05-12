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
2. **Project-Level**: Significant changes must be mirrored in the root `Resources/changelog.md` file.
3. **Traceability**: The summary in the file header should provide enough context to understand *why* the change was made, while the project changelog provides the *what* for the end-user.

---

## ⚖️ Integrity Rules

- **Never Delete**: Changelog entries are permanent. Do not remove old entries to save space.
- **Accuracy**: Ensure dates and version numbers match the official release tags.
- **Consistency**: Use the same terminology across both the file header and the project-wide changelog.

---

**RecoveryCommander** — *Traceable, Auditable, Reliable.*
