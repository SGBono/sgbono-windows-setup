# SGBono Windows Setup

Post-install script for the deployment of laptops by SGBono

# Equipment required

- File server running Windows Server 2022 (though realistically a Linux file server could also be used)
  
- Router (disconnected from Internet)
  
- Ethernet cable connecting from router to server
  

# How this works

- Online Install (default)
  
  - Drivers download from Windows Server
    
  - Iterates through defined XML file (ProgramsList.xml) on the Windows Server
    
  - Installs required programs through setup files on the Windows Server
    
  - Generates system report (which includes battery health and machine's specifications)
    
  - Deletes all traces of the program, including router credentials, temporary files and the app itself once complete
    
- Offline Install (fallback)
  
  - Drivers installed from Drivers USB
    
  - Programs NOT installed (use recovery software to do this)
    
  - Generates system report (which includes battery health and machine's specifications)
    
  - Deletes all traces of the program, including router credentials, temporary files and the app itself once complete
    

# Security Disclaimer

It is PARAMOUNT that all equipment hosting the required files for Online Install be secured to the highest degree possible. Due to the nature of Online Install, a compromised Windows Server or router means that a MASSIVE Remote Code Execution exploit would be opened up, and a physical attacker will be able to execute ANY command on the machines we are refurbishing.

Strict precautions MUST be made as the security of these machines we are giving out has to be protected at all costs. Such measures include:

- NO CONNECTION TO THE INTERNET
  
- Proper access control on Windows Server (read only mode for SMB connections)
  
- Windows Server MUST have security updates installed as soon as they are available
  
- Windows Server will have an anti-virus solution installed as an extra layer of defence
  
- Router password must use secure protocols such as WPA-2 wherever possible (eg. avoid WPA)
  
- Regular pentesting
  

## We would also like to remind all volunteers to remain vigilant and report ANY suspicious behaviour IMMEDIATELY to the head developer (Chin Ray) or the coordinator of volunteers (Xuan Han). 

## Look out for rogue access points matching our router's SSID, discrepancies with the software's functionality or unusual behaviour with the Windows install in general.

# Using this app

This app has already been pre-packaged into the custom ISO files that we use for deploying Windows onto the machines we are refurbishing. This app will launch automatically right after logging in.

[Content here to be filled up]
