# Codelines - a dotnet tool to calculate your lines of code performance

A CLI tool for counting lines of code in your projects, excluding empty lines and generated code.

## Description

CodeLines is a console application for .NET developers that helps analyze projects and get statistics on code line count. The tool ignores empty lines and can be configured to exclude generated code or other files based on patterns.

## Installation

```bash
dotnet tool install --global CodeLines
```

## Core Commands

### Initialization

```bash
cline init [--path|-p <project_directory>] [--force|-f]
```

Initializes a project directory by creating a `.clineIgnore` file to configure which files and directories to ignore.

Parameters:
- `--path, -p` - path to the project directory (default: current directory)
- `--force, -f` - force create a new `.clineIgnore` file, overwriting any existing one

### Scanning

```bash
cline scan [--path|-p <project_directory>] [--diff|-d] [--verbose|-v]
```

Scans the project and calculates the lines of code.

Parameters:
- `--path, -p` - path to the project directory (default: current directory)
- `--diff, -d` - shows difference with previous scan results
- `--verbose, -v` - provides detailed output

### Scan History

```bash
cline history [--path|-p <project_directory>] [--last|-l <number_of_entries>]
```

Shows the history of project scans.

Parameters:
- `--path, -p` - path to the project directory (default: current directory)
- `--last, -l` - number of recent history entries to display

### Managing Ignored Files

#### Add Pattern

```bash
cline ignore add <pattern> [--path|-p <project_directory>]
```

Adds a pattern to the `.clineIgnore` file.

#### Remove Pattern

```bash
cline ignore remove <pattern> [--path|-p <project_directory>]
```

Removes a pattern from the `.clineIgnore` file.

#### List Patterns

```bash
cline ignore list [--path|-p <project_directory>]
```

Shows the list of ignored patterns.

## Usage Examples

### Basic Scan of Current Directory

```bash
cline scan
```

### Scan with Difference Display

```bash
cline scan --diff
```

### Adding an Ignore Pattern

```bash
cline ignore add "**/*.Designer.cs"
```

### View Last 5 Scans

```bash
cline history --last 5
```

## .clineIgnore File Format

The `.clineIgnore` file uses syntax similar to `.gitignore`:

```
# Comment
bin/
obj/
**/*.Designer.cs
**/Generated/*.cs
```

## License

MIT
