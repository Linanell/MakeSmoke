# Make smoke

**MakeSmoke is a program for simple parsing sites.**

This program collects all links, errors and redirections in json files.

Data for work can be entered using .xml file or using bash command.
Bash command may have higher priority than .xml file. If the file is already in use, then bash command may overwrite the settings entered in the file.



Chromedriver version used in project: `129.0.6668.10000`

## How to Run:

Parameters for program can be set via settings.xml file or via a bash command.
Example file is already in the project. It describes all the parameters available for settings.xml file.
**settings.xml** file must be located in the folder with the compiled project.

At least a URL to parse is required.

### Bash Parameters:

- `-d` - Enable debug mode with additional output messages.
- `-r` - Enable recursive mode. With this option, the parser will automatically parse all links on pages from the site.
- `--filter=*part of url*` - Set a custom URL for detecting internal/external links. URLs that do not belong to the filter URL will be marked as external and ignored.
- `--threads=*number of threads*` - Set custom number of browser threads for asynchronous parsing (default: 5).
- `--name=*name of log file*` - Set custom name for log files (default: makesmoke).
- `--blacklist=*black list file name*` - Set file name for part of messages that should be excluded (like blacklist.json file).
- `--verify=*links to verify file name*` - Set file name for links that should be checked for an actual existence (like verify-links.json file).
- `*URL without parameters*` - The URL to parse.

### Bash Launch Example:

```bash
MakeSmoke.exe -r --threads=5 --verify=verify-links.json --name=example_site https://example.com/pageToParse
```

## Black list and verify

Unwanted errors can be excluded from the list. For this purpose, a file is used that can be specified using **--blacklist=** bash parameter or **BlackList** xml parameter.
The json file used must contain an "exclude" key that will list a parts of an excluded errors. An example is given in the blacklist.json file.

Program can also check at the end of its work that it has found all necessary links. For this purpose, a file is used that can be specified using **--verify=** bash parameter or **VerifyLinks** xml parameter.
The json file used must contain an "verify" key that will list a links that should be verified. An example is given in the verify-links.json file.

The same json file can be used for there purposes. Both keys must be specified in the file like:

```
{
  "verify": [
    "https://example.com",
    "https://example.com/page1",
    "https://example.com/page2",
    "https://example.com/blog/post1",
    "https://example.com/blog/post2",
    "https://example.com/about",
    "https://example.com/contact",
    "https://example.com/shop/product1",
    "https://example.com/shop/product2",
    "https://example.com/help/faq"
  ],
  "exclude": [
    "webpack",
    ".jpg"
  ]
}
```