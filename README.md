# __Spindler: The Newer Spindle__
An app to simplify web scraping for books and other 'written' media

## Configurations
Add configurations to the configuration screen to inform the app what to scrape

## General Configurations
Add a path to check the structure of a webpage. If it _matches_, the general configuration will be used. Normal configurations are prioritized.

### Supports Csspaths and Xpaths natively
#### Csspaths

If you add a _$_ after a normal css path, it will target that attribute.

`a.chapter $href` will target the `href` attribute of an `<a>` tag with the class 'chapter'.

For paths, the href attribute is prioritized, so if you want to get the text content of the `<a>` tag, you can use the modifier `$text`.

#### Xpaths

No extra features for xpaths; all basic xpath schemes should be supported by default.

## Getting Cookies

Some websites need cookies to function properly. Websites protected by _cloudflare_ for example, require a browser check, which will automatically fail. 

By turning on the __Get Cookies__ switch in your configuration options, you should be able to get to a webview with a __Get Cookies__ button. Press this button when you can see the text content of the page, and it should save your cookies. 

__Warning__: This is an experimental feature. File a bug report if this fails. If you can figure out why, that I would be even better. 