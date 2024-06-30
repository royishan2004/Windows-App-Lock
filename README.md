# Windows App Lock

Tired of your friends and collegues opening your personal messages and pics while they are borrowing your computer 🥲? 

Introducing Windows App Lock (WAL) - your trusty and personal `.NET` app to lock custom desktop applications with the ease and convenience of `Windows Hello` and the familiar Windows 11 UI through `WinUI3`.

# Features

- Lock specific applications installed on your device as per your wish.
- Maintain comprehensive logs on the access requests and settings of locked applications.
- Interactive notifications to keep you updated on important security actions.
- Authentication settings for the control app so that you can choose for your securtiy setup to not compromised with.
- Convenient management settings like compatibility check, auto-start options, etc.
- A comprehensive help page with support contact options.
- User-friendly, sleek and native `WinUI3` User Interface.

# Get Started

> As this application is still under development, the creation of stable and final release is still in progress. Till then, you can `clone` the project to your device and use it under `debug` mode. Please check again later for a `release` version on this page.


## Installation

Follow the below instructions to set up the project:

<ol>
 <li> Clone the repository to your device.
  
  ```sh
  git clone https://github.com/royishan2004/Windows-App-Lock.git
  ```
 </li>
 <li> 
  
  Open the solution file `WindowsAppLock.sln` on Visual Studio. </li>
  <li> Resture missing Nuget packages. Eg:
   
   ```sh
   dotnet add package CommunityToolkit.WinUI.UI.Controls.DataGrid 
   ```
  </li>
  <li> Build the solution.</li>
  <li> Deploy the app on your device.</li>
</ol>

## Usage

<ol>
 <li> Launch the application. </li>
 <li>

  Go to the `Settings` page to configure your preferences.
 </li>
 <li> 
  
  Add the applications desired to be locked in the `App List` page. </li>
  <li> 
   
   The application will monitor the specified applications and prompt for `Windows Hello` authentications whenever necessary. For a record of the authentication requests and settings modifications, go to the `Activity Logs` page.</li>
<li> 
  
  Check out the `Help` page for more assistance. </li>
</ol>

# Gallery

<details closed>
<summary><h3>Screenshots</h3></summary>
</details>

# Requirements

## Software

- `Windows 10 Version 1607` or above | `Windows 11 Version 2H22` or above
- `.NET 6.0` or above
 > Backward Compatibility for previous .NET versions will be introduced in the releases.
- `Visual Studio Community 2022` with `.NET Desktop Development` workload installed. **[FOR DEBUG ONLY]**
- `Windows Hello` enabled and setup.

## Hardware

- Basic CPU, RAM, HDD/SSD configuration to run `Windows 10 Version 1607` and above
- Fingerprint Sensor **[OPTIONAL]**
- Webcam supporting Facial recognition **[OPTIONAL]**
