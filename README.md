# Base Station Power Management

Using [nouser2013's](https://github.com/nouser2013) [lighthouse-v2-manager](https://github.com/nouser2013/lighthouse-v2-manager) python script and snippets from [BOLL7708's](https://github.com/BOLL7708) [OpenVRStartup](https://github.com/BOLL7708/OpenVRStartup) automatic command file executor.

Licenses for bundled code are found in their respective folders inside `./include/`

---

**Required dependencies:**
* .NET 6.0 Required. Not bundled into executable
* Windows 10, at least 17xx build
* BLE / Bluetooth 4.0 dongle installed and connected (not a BGAPI one!)
* Python 3
    * Pythonnet `pip3 install pythonnet`
    * bleak `pip3 install bleak`
* FluentAssertions
* xunit

---

> **CAUTION:** Installation instructions are not correct. There is no way of inputting your base station mac addresses without building the application yourself **yet..** This feature will be implemented soon with GUI and more!

## Installation:

1. Download latest binary from the [Releases](https://github.com/TitusStudiosMediaGroup/basestation_power_management/releases) page
2. Execute binary atleast once before using with SteamVR.
3. You're done! Every time you launch SteamVR the executable will launch in the background, and close on exit of SteamVR.

> **NOTE:** Launching the executable on its own will launch SteamVR
