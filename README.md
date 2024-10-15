# Make smoke

**MakeSmoke is a program for simple parsing sites.**  
Chromedriver version: `129.0.6668.8900`

## How to Run:

At least a URL to parse and the number of threads are required.

### Available Parameters:

- `-d` - Enable debug mode with additional output messages.
- `-r` - Enable recursive mode. With this option, the parser will automatically parse all links on pages from the site.
- `--filter=*part of url*` - Set a custom URL for detecting internal/external links. URLs that do not belong to the filter URL will be marked as external and ignored.
- `--threads=*number of threads*` - Set custom number of browser threads for asynchronous parsing (default: 5).
- `--name=*name of log file*` - Set custom name for log files (default: makesmoke).
- `*URL without parameters*` - The URL to parse.

## Example:

```bash
MakeSmoke.exe -r --threads=5 https://example.com/pageToParse