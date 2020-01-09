# Install ShortUrl on Linux with systemd and nginx

# File stage
1. Build this project to get the binary output. Or, get the built version directly.
2. Create a folder for this service on server. The path of this folder should override <path/to/the/folder> in the code below and the configuration files related.
3. Copy binary files to this folder.
4. Change permission by these commands:
```sudo chown -R www-data:www-data <path/to/the/folder>
sudo chmod -R 755 <path/to/the/folder>
```
