# Install ShortUrl on Linux with systemd and nginx

# File stage
1. Build this project to get the binary output. Or, get the built version directly.
2. Create a folder for this service on server. The path of this folder should override <path/to/the/folder> in the code below and the configuration files related.
3. Copy binary files to this folder.
4. Change permission by these commands:
```
sudo chown -R www-data:www-data <path/to/the/folder>
sudo chmod -R 755 <path/to/the/folder>
```

# Systemd stage
1. Create a service file in /etc/systemd/system, named as shorturl.service.
2. Place the text from [the example](shorturl.service) to this service file, replacing <port> to the number of a chosen free tcp port, and the <path/to/the/folder> as well.
3. Reload systemd by ```systemctl daemon-reload```.
4. Start this service by ```systemctl start shorturl.service```.
5. If everything goes will, you can see the port is listed in ```lsof -i -P -n | grep LISTEN```. At last, set this service to start with system by ```systemctl enable shorturl.service```.

# Main configuration stage
All settings of ShortUrl is saved in file ```SecretNest.ShortUrl.Setting.json``` placed in the working folder specified by systemd service file. If the service is started without the file, the file will be created with default value. Before changing the file, the service should be stopped at first.

To change some core setting, which cannot be changed by management page, follow these steps:

1. Stop the service by ```systemctl stop shorturl.service```.
2. Edit the file ```SecretNest.ShortUrl.Setting.json```.
3. ```enableStaticFiles``` could be set to ```true``` or ```false```. When enabled, all requests matched with path of a static file located in the folder specified will be served with the file data directly.
4. ```userSpecifiedStaticFileFolder``` could be set to a path of a folder, which is used to place all static files. This value is read only when ```enableStaticFiles``` is set to ```true```. If this is set to ```null``` or empty string, the default value (```wwwroot``` folder under the service folder) will be used.
5. ```preferXForwardedHost``` could be set to ```true``` or ```false```. When enabled, host name is read first from the ```X-Forwarded-Host``` of the header. When using ShortUrl after a proxy, this value should be set to ```true```. Otherwise, set this value to ```false``` for security purposes.
6. All other settings can be changed by management page. Changing value in file is NOT recommended unless you cannot use Https protocol for managing.
7. You should note the value of ```globalManagementKey``` for entering the management page.

# Nginx stage
1. Create a site file in ```/etc/nginx/sites-available``` named as you wish, like ```example.com``` for example.
2. Choose from one.
   - If you want to use certbot later, place the text from [the example](nginx.http) to this site file, replacing <port> to the number of the chosen tcp port for service, and the <SERVER_DOMAIN> to the domain name or names. You can use http only for servicing as well. But for using Http on management is hightly NOT recommended.
   - If you want to use your own Https certificate, place the text from [the example](nginx.https) to this site file, replacing <port> to the number of the chosen tcp port for service, the <SERVER_DOMAIN> to the domain name or names, and the paths of the certificate files.
3. Enable this site by ```ln -s /etc/nginx/site-available/example.com /etc/nginx/site-enabled```.
4. Test config by ```nginx -t```.
5. Reload ngxin by ```systemctl reload nginx```.
6. (Optional) Use certbot to enable the https for this site. Http redirection is not required.

# Test
By navigating to the server domain, you should be redirect to the default page (google.com in default config).

# Management
Enter the management page by navigating to ```https://yourdomain/<globalManagementKey>```.

This domain should be listed as global management enabled hosts. All domains are enabled if this list is empty.

See [this document](../../management) for details.
