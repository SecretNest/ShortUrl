# UNDER CONSTRUCTING
Code is not finished and tested. Do NOT use it at this moment.

# ShortUrl
Short url redirector with no database required. DotNet Core 3.1 version.

Language: C#

Platform: ASP.Net Core 3.1

# Features

- No database required.
- Multiple host names supported.

# Installation

* [Linux with systemd and nginx](deployment/Linux).
* [Windows with IIS](deployment/Windows).

# Management

* [Management](management)
* Edit the configuration file directly when HTTPS is not available.

# Work flow
1. Gets the ```host``` from the HTTP Header.
   - When ```preferXForwardedHost``` in configuration file is set to ```true``` and ```X-Forwarded-Host``` exists in HTTP Header, gets the ```host``` from ```X-Forwarded-Host```.
   - Gets the ```host``` from ```Host``` in HTTP Header.
2. Gets the ```path segments``` from the HTTP Header as the ```access key```.
3. Enters the Global Management page when
   - The ```access key``` equals to ```Global Management Key```. And,
   - The ```host``` is allowed to enter Global Management page when
     - The ```host``` exists(*1) in ```Global Management Enabled Hosts```. Or,
     - The list ```Global Management Enabled Hosts``` is empty.
4. Resolves by aliases when the ```host``` equals(*1) the ```Alias``` column of one record in ```Aliases``` from Global Management, choosing ```Target``` as the new value of ```host``` and restart this step. Aliases could be resolved recursively with 16 as the max depth.
5. Processes the request against the domain when the ```host``` equals(*1) ```Domain``` column of one record in ```Domains``` from Global Management.
   1. Enters the Domain Management page when ```access key``` equals to ```Domain Management Key```.
   2. Resolves by redirects when the ```access key``` equals(*2) the address column of one record in Redirects from Domain Management of this domain.
      - When the ```Target``` from the record matched is starting with ```>```, gets the text after ```>``` from ```Target``` as the new value of ```access key``` and restart this step. Redirects could be resolved recursively with 16 as the max depth. Or,
      - Redirects to ```Target``` specified of the record matched.
   3. Redirects to ```Default Redirect Target``` specified in Domain Management of this domain if it is not empty.
6. Redirects to ```Default Redirect Target``` specified in Global Management.

*1: Host name matching rules:
- Host name matching is case insensitive.
- Port number 80 or 443 should not present, but all others should and will be treated separately. For example:
  - The record "example.com" will be matched with the host "example.com", "example.com:80" and "example.com:443".
  - The record "example.com:8080" will be matched with the host "example.com:8080" only.
- The record with the key ends with ":80" or ":443" in domains or aliases will not be matched unless it's pointed by other matched alias records.

*2: Name matching could be case sensitive or insensitive, based on the setting ```Ignore Case When Matching``` specified in the Domain Management of the related domain.

When redirecting:
- HTTP 301 will be used, when ```Use HTTP 301 instead of 302``` or ```Use HTTP 301``` is selected. Or HTTP 302 will be used.
- When ```Attach Query Process``` is enabled and the query string exists from the request:
  - When character ```?``` presents in the target of the redirection, ```&``` and the query string from the request will be appended.
  - When character ```?``` absents from the target of the redirection, ```?``` and the query string from the request will be appended.
- When ```Attach Query Process``` is disabled, the query string, if exists, from the request will be dropped and will not be passed into the redirection target.
