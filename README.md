# cmdbot
Command line powered by [streamer.bot](https://streamer.bot)

## What is the function of this program?
cmdbot is a console application that can give commands to streamer.bot via commands. And can also control stremer.bot via 2nd keyboard like HIDmacros and AutoHotInterception.

## How to use
### Installation
As easy as download, and extract

### Initial setup
Go to the place where you extracted cmdbot, open `cmdbot.exe` to create the config file

Open `cmdbot.exe` as administrator so you can use cmdbot everywhere, and restart for run properly (like ability to open in vbs file)

don't forget to open `streamer.bot.exe` and connect **HTTP server** in **Servers/Clients** tab to run it 

![streamer.bot](https://github.com/futamiyarn/cmdbot/blob/main/assets/Streamer.bot_Q7oLxIQeMI.png?raw=true)

### Usage
#### Show the list
```
cmdbot --list
```
or `cmdbot -l`, use `--addr` with your `address:port` for defrent streamerbot client or changed in `config.json`

#### Save command
```
cmdbot save <your action>
```
By default, action will be saved in `C:/users/(username)/Documents/cmdbot`, You can save multiple actions at once in different places. For help, run `cmdbot save --help` for more commands

#### Execute the action
```
cmdbot run --action <action name://action id (uuid)> <variable:"here">
```
You don't need to remember the command because you can open bat/vbs, or use applications like [HID Macros](https://www.hidmacros.eu/download.php) or [AutoHotkey](https://www.autohotkey.com/)/[AutoHotInterception](https://github.com/evilC/AutoHotInterception).

#### Example AutoHotkey syntax 
- AutoHotkey v1
```ahk
Run, %ComSpec% /c cscript //nologo "C:\Users\Futami\Documents\cmdbot\A Groups\Action.vbs", , Hide
```

- AutoHotkey v2
```ahk2
Run(ComSpec, '/c cscript //nologo "C:\Users\Futami\Documents\cmdbot\A Groups\Action.vbs"', "Hide")
```
Use **vbs** file to run without displaying the window/program