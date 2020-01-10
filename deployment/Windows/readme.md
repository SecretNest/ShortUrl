# Install ShortUrl on Windows with IIS

# System stage
This server should has Windows installed with IIS role and ASP.NET Core Runtime 3.1.0.

# File stage
1. Build this project to get the binary output. Or, get the built version directly.
2. Create a folder for this service on server. The path of this folder should override <path/to/the/folder> in the code below.
3. Copy binary files to this folder.
4. Make sure the account which will be specified as the Application Pool Identity has the permission to create, read, write and delete files within this folder.

# IIS stage
1. Create a new Application Pool, with ```.NET CLR version``` set to ```No Managed Code```, using the account related in step 4 of File stage as the Identity, and start it.
2. Create a web site with the folder created in step 2 of File stage and Application Pool created in the step 1 above.
3. Bind at least one domain with this site.
4. Add HTTP and/or HTTPS ports to this site. Using HTTP only for servicing is acceptable. But using HTTP for management pages is highly NOT recommended.
5. Start this site.

# Main configuration stage
All settings of ShortUrl is saved in file ```SecretNest.ShortUrl.Setting.json``` placed in the folder of this service. If the service is started without the file, the file will be created with default values. Before changing the file, the service should be stopped at first.

To change some core setting, which cannot be changed by management page, follow these steps:

1. Stop the site.
2. Stop the application pool related.
3. Edit the file ```SecretNest.ShortUrl.Setting.json```.
4. ```enableStaticFiles``` could be set to ```true``` or ```false```. When enabled, all requests matched with path of a static file located in the folder specified will be served with the file data directly.
5. ```userSpecifiedStaticFileFolder``` could be set to a path of a folder, which is used to place all static files. This value is read only when ```enableStaticFiles``` is set to ```true```. If this is set to ```null``` or empty string, the default value (```wwwroot``` folder under the service folder) will be used.
6. ```preferXForwardedHost``` could be set to ```true``` or ```false```. When enabled, host name is read first from the ```X-Forwarded-Host``` of the header. When using ShortUrl after a proxy, this value should be set to ```true```. Otherwise, set this value to ```false``` for security purposes. For being accessed directly, this value can be set to ```false``` under IIS environment.
7. All other settings can be changed by management page. Changing value in file is NOT recommended unless you cannot use HTTPS protocol for managing.
8. You should note the value of ```globalManagementKey``` for entering the management page.

# Test
By navigating to the server domain, you should be redirect to the default page (google.com in default config).

# Management
* Enter the Global Management page by navigating to ```https://yourdomain/<globalManagementKey>```. 
  - This domain should be listed as global management enabled hosts. All domains are enabled if this list is empty.
  - ```<globalManagementKey>``` can be obtained from the configuration file by the step 7 of Main configuration stage.
  - ```<globalManagementKey>``` is displayed and can be changed in Global Management page.
* Enter Domain Management page by navigating to ```https://yourdomain/<domainManagementKey>```.
  - By pressing the Manage button after the domain listed in Global Management page, browser will open this page as well.
  - ```<domainManagementKey>``` can be obtained from the configuration file.
  - ```<globalManagementKey>``` is displayed and can be changed in Domain Management page.

See [this document](../../management) for details.
