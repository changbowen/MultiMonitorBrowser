# MultiMonitorBrowser
A quick small project focused on improving the automation level of the monitoring service, while minimizing the maintenance overhead.
Depending on how it is configured, the WPF based application can be used to automatically display a full-screen website on each monitor after the system is booted.

## System Requirements
The application has been tested on the following environments. Only x64 version is being built currently.

| OS | .Net Framework |
| --- | --- |
| Windows 10 x64 | .Net Framework 4.6.1 |

## Design, Develop and Deploy
### Features Implementation
- Basic WPF application targeted to .Net Framework 4.6 (and later).
- CefSharp is used to provide a Chromium core instead of IE that is built-in to the framework.
- Support running a custom piece of JavaScript after the page is loaded. This can be used for logging in to various sites.
- Encrypted strings are supported to embed sensitive information in the JavaScript like passwords.
- Favicon.ico from the displayed website is used as the browser window icon.
- Hide the mouse cursor.
- Configuration file is reloaded and applied every minute.
- Automatic supply credentials for basic authentication.
- ClickOnce deployment.
- High DPI per-monitor aware.
- Subscribe to display and config file change events and update windows accordingly.

### Deployment
The configuration file will be created during the first launch, after which you are required to customize the configuration file with your own source address and credentials.
Refer to config_sample.xml file for more information.
To generate the encrypted values, use the shortcut in Start Menu -> Carl Chang -> MultiMonitorBrowser Encrypt (right next to the application shortcut).
Encryption uses the RijndaelManaged class with the encryption keys set in the configuration file.

If the number of configured monitors is larger than the number of available physical monitors, excessive ones will be ignored. Same rule applies when physical monitors are more than configured monitors.
