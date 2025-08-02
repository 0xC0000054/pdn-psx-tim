# pdn-psx-tim

A [Paint.NET](http://www.getpaint.net) filetype plugin that adds support for loading the PSX TIM format.

## Installing the plugin

1. Close Paint.NET.
2. Place PsxTimFileType.dll in the Paint.NET FileTypes folder which is usually located in one the following locations depending on the Paint.NET version you have installed.

  Paint.NET Version |  FileTypes Folder Location
  --------|----------
  Classic | C:\Program Files\Paint.NET\FileTypes    
  Microsoft Store | Documents\paint.net App Files\FileTypes

3. Restart Paint.NET.

## License

This project is licensed under the terms of the MIT License.   
See [License.txt](License.txt) for more information.

***

# Source code

## Prerequsites

* Visual Studio 2022
* Paint.NET 5.0.0 or later

## Building the plugin

* Open the solution
* Update <PdnRoot> in PsxTimFileType.csproj file to match your Paint.NET install location
* Ensure the Paint.NET FileTypes folder is writeable, because post build events copy the dll there
* Build the solution
