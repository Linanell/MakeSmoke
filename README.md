MakeSmoke is a program for simple parsing sites.
Chromedriver - 129.0.6668.8900.

How to run:
At least URL to parse and number of threads are required.
Parameters avaliable:
-d - Enable debug mode with additional output messages.
-r - Enable recursive mode. With this option parser will automatically parse all links on pages from this site.
--filter=*part of url* - Set a custom url for detecting internal/external url. Url that doesn't belongs to filter url will be marked as external and ignored.
--threads=*number of threads* - Set a number of browser threads for async testing.
*URL without parameters* - URL to parse.

Also avaliable "settings.xml" file for loading parameters from file. See "settings.xml" file in project for details.