# Spindler: The Newer Spindle
An app to simplify web scraping for books and other 'written' media

## Configurations
Add configurations to the configuration screen to inform the app what to scrape
#### Supports Csspaths and Xpaths natively
__Csspaths__
If you add a _$_ after a normal css path, it will target that attribute
```a.chapter $href``` will target the href attribute of an <a> with the class 'chapter'
