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

When the host supplied in HTTP Header is not resolved by aliases and / or is other than existing domain records, the request will be redirected to the target specified.

### Global Management Key
Changes the ```Global Management Key```. This will reload the whole page with the new key applied as the path segment of url to be requested.

### Global Management Enabled Hosts
Sets the hosts allowed to enter Global Management page.

All hosts are allowed when the list is empty.

When setting up, the current host must be added to the list first to allow the rest operating from this host. And due to the same reason, when removal, the current host must be the last one to be removed.

## Domains
Adds or removes domain, or navigates to Domain Management page of the selected domain.

To add a new domain, enter the domain name and press Add Domain button.
For domain removal and management, press the Remove or Manage button after the domain related.

## Aliases
Adds, updates or removes domain aliases.

To add a new alias, enter the alias (key) and target, then press Add Alias button.
To update an alias with a new key and / or target, change the values and press Update button after the alias related.
To remove an alias, press Remove button after the alias related.

The target can be the Domain under Domains block, or another Alias under Aliases block for recursive resolving with 16 as the max depth.

## Domain name and alias key
- Domain name and alias key matching is case insensitive.
- Port number 80 or 443 should not presents, but all other should and will be treated separately. For example:
  - The record "example.com" will be matched with the host "example.com", "example.com:80" and "example.com:443".
  - The record "example.com:8080" will be matched with the host "example.com:8080" only.
- The record with the key ends with ":80" or ":443" in domains or aliases will not be matched unless it's pointed by one matched alias record.
- More than one record from domains and aliases with the same name, or names with only different case, are not allowed.

# Domain Management page

Domain Management page is designed for managing redirects of one domain and setting default redirection for the requests not matched with any redirects in this domain. 

## Domain Setting
### Default Redirect Target
Sets an target for default redirection.

When the path segment is not resolved by redirection records, the request will be redirected to the target specified.

### Domain Management Key
Changes the ```Domain Management Key``` of this domain. This will reload the whole page with the new key applied as the path segment of url to be requested.

### Ignore Case When Matching
Sets the name matching rules of redirection record should be case sensitive or not.

After enable this, the records with similar names with different case will be kept only one. If some records are removed by this action, the whole page will be reloaded.

## Redirects
Adds, updates or removes redirection records.

To add a new record, enter the address and target, select whether it should use HTTP 301, select how to treat query string, then press Add Redirect button.
To update a record, change the values and press Update button after the record related.
To remove a record, press Remove button after the record related.

The target can be:
- A text starting with ```//```: Redirects to this domain name, with path segments if presents, and query string if presents, using the same protocol as the user request. This should be the most common.
- A text starting with ```http://``` or ```https://```: Redirects to this domain name, with path segments if presents, and query string if presents, using the protocol specified.
- A text starting with ```>```: Marks this record as an alias to another one with the address equals the text after ```>```. Redirects could be resolved recursively with 16 as the max depth.
- A text in other format: Redirects to the new place using this text as path segment.

When redirecting:
- HTTP 301 will be used, unless ```Use HTTP 301 instead of 302``` or ```Use HTTP 301``` selected. Or HTTP 302 will be used.
- When ```Attach Query Process``` is enabled and the query string exists from the request:
  - When character ```?``` presents in the target of the redirection, ```&``` and the query string from the request will be appended.
  - When character ```?``` absents from the target of the redirection, ```?``` and the query string from the request will be appended.
- When ```Attach Query Process``` is disabled, the query string, if exists, from the request will be dropped and will not be passed into the redirection target.
