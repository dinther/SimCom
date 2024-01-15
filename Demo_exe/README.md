# Demo EXE files for Windows

Just download these zip files and extract the content on your drive. EXE files are contained in a folder named after the zip file.

The Demo programs will automatically install the required library files in the Microsoft Flight Simulator community folder.
You probably need to restart MSFS if it was already running.

Run both MSFS and the demo program.

The demo also fully works across a LAN. Two files to edit in the demo application folder:
client_conf.ini  -  Change the networkConfigId value from -1 to 3

simconnect.cfg

Find the [SimConnect.3] in the file. The number 3 refers back to the networkConfigId in the client_conf.ini file.
Make it look like this
```
[SimConnect.3]
Protocol=Ipv4
Address=127.0.0.1  <---Change this to the IP Address of the PC running MSFS.
Port=500
MaxReceiveSize=41088
DisableNagle=1
```

The last two parameters seem to make all the difference in my testing. All this is assuming that the SimConnect.xml that came with MSFS has not been modified. Basically there needs to be a match between a SimConnect.Comm entry in the SimConnect.xml file and the entry in the simconnect.cfg file.
Also make sure to set the firewall on your Flight sim PC to allow incoming connections on Port 500.

I intend to write some code for this later because it's just too painful. But it does work.
