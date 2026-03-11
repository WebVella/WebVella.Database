# Copilot Instructions

## Project Guidelines
- Never use "// Arrange", "// Act", "// Assert" comments or any combination of these terms in unit test generation.
- Format all code to fit within 120 characters per line for this repository.
- Method definitions and XML documentation comments should fit within 120 characters per line.
- When method signatures exceed 120 characters, split parameters into groups that fit within 120 characters per line.
- When creating objects with initializers, place each property on a new line.
- Use region format: `#region <=== TEXT ===>` for all region declarations.
- Never delete source code files using PowerShell commands, especially if the file is locked by another process. If a file operation fails due to a lock, fail the request instead of attempting to delete or recreate the file.