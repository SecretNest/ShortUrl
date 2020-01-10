# Management
Nearly all settings could be changed by management pages. ```SecretNest.ShortUrl.Setting.json``` will be updated after setting changed through management pages.

Using management pages is recommended unless HTTPS is not available. Using HTTP for management pages is highly NOT recommended.

All settings of ShortUrl is saved in file ```SecretNest.ShortUrl.Setting.json``` placed in the folder of this service. If the service is started without the file, the file will be created with default values. Before changing the file, the service should be stopped at first. This file will be loaded only while service starting.

# Management Key

Management keys are the only validation for management, which should be kept in secret.

* Global Management key
  - only has one.
  - is the key for entering the Global Management page.
  - can be obtained from the configuration file.
  - is displayed and can be changed in Global Management page.
* Domain Management key
  - can be set separately for each domain name.
  - is the key for entering the Domain Management page of the related domain.
  - can be obtained from the configuration file.
  - is used as part of the url of the Manage button in Domains block of Global Management page.
  - is displayed and can be changed in the Domain Management page of the related domain.

## Using HTTP for management pages

Using HTTP for management pages is highly NOT recommended due to

* Global Management key will be leaked as a part of the address and the html content of the Global Management page.
* Domain Management key(s) could be captured as a part of the html content of the Global Management page.
* Domain Management key will be leaked as a part of the address and the html content of the Domain Manage page of the related domain.

# Global Management page

Global Management page is designed for managing domains and setting default redirection for the requests from domains other than exists.

## Global Setting
### Default Redirect Target
Sets an target for default redirection.

When requests received with the host supplied in HTTP Header is not resolved by aliases and / or is other than existing domain records, the request will be redirected to the target specified.

## Domains


## Aliases



# Domain Management page

