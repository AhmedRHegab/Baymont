# Windows Firewall (LAN only)

1. Open **Windows Defender Firewall with Advanced Security**.
2. Inbound rule: allow TCP 80 and/or 443.
3. Scope → Remote IP address → These IP addresses → add village LAN subnet only  
   Example: `192.168.10.0/24`
4. Do **not** allow "Any" remote addresses on a production village deployment.
5. Bind the IIS site to the server LAN IP (not `0.0.0.0` publicly reachable interfaces if a WAN NIC exists).
