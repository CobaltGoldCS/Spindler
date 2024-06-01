# Architecture of Spindler

Spindler uses .net MAUI and C# for all parts of the program. Standard practice for .net MAUI is to use *MVVM* as the architectural framework of any programs. Most pages of Spindler follow this rule, with views accessing a viewmodel for functionality, which then accesses a model of some sort to drive the data.

**However**, some pages are simple enough that the code-behind file of each page is used as the viewmodel instead,like in the cases where Website Configurations are created and updated.

Spindler also uses the provided MAUI dependency injection framework, which will inject dependencies into the pages and many of the viewmodels. It generally injects *services* such as the database and http client.

Navigation is controlled by MAUI's SHELL system, which is URI-based. These URIs are defined in `MauiProgram.cs` and `App.xaml.cs`. Most navigation is handled in the `SpindlerViewModel` class.

# Scraping Pipeline

1. When a book url is accessed for reading, the hostname is chopped out and checked against all known Configs. If there is no match found, the page will be loaded by a headless "browser" (webview hidden from the user), and the generalized configs will attempt to match the url using their *match paths*.
2. Once a configuration is matched, the system will take the html of the page, and extract the various components of the chapter using paths provided by the given configuration
3. The main text of the page is further filtered according to the *extractor scheme* specified in the configuration. Examples include all html tags matching the content path, the first html tag matching the path (default) as well as plaintext extraction
4. Finally the content is displayed to the user.

#### Preloading chapters
Once either the next or previous chapter button is pressed, the system is designed to begin preloading the content of the chapter. This will ideally result in the next/previous chapter being instantaneously loaded from the user's perspective. 
