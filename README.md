# Lunar.OCR

<div align="center">
    <img src="https://gclstorage.blob.core.windows.net/images/Lunar.OCR-banner.png" />
</div>

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Donate](https://img.shields.io/badge/$-donate-ff69b4.svg)](https://www.buymeacoffee.com/chunlin)

This is a project for demonstrating how Tesseract and Azure Computer Vision can be used in an UWP app to perform Optical Character Recognition (OCR). 
The app now supports three languages, i.e. English, Simplified Chinese, and Korean.

Currently, the UWP app works fine with Tesseract on Windows 10 PC but no on Hololens 2 (Emulator). Hence, the app will automatically retry OCR with
Azure Computer Vision if Tesseract fails.

## Key Technologies ##
1. Windows 10 (Version 1903 - Version 2004)
1. [NuGet - Tesseract](https://www.nuget.org/packages/Tesseract/) (Version 4.1.1)
1. [Azure Computer Vision](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/) (Version 7.0.0)
1. Hololens 2 Emulator

## Screenshots ##
<div align="center">
    <img src="https://gclstorage.blob.core.windows.net/images/Lunar.OCR-screenshot-01.png" />
</div>

<div align="center">
    <img src="https://gclstorage.blob.core.windows.net/images/Lunar.OCR-screenshot-02.png" />
</div>

<div align="center">
    <img src="https://gclstorage.blob.core.windows.net/images/Lunar.OCR-screenshot-03.png" />
</div>

<div align="center">
    <img src="https://gclstorage.blob.core.windows.net/images/Lunar.OCR-screenshot-04.png" />
</div>

## Contributing ##
First and foremost, thank you! I appreciate that you want to contribute to this project which is my personal project. Your time is valuable, and your contributions mean a lot to me. You are welcomed to contribute to this project development and make it more awesome every day.

Don't hasitate to contact me, open issue, or even submit a PR if you are intrested to contribute to the project.

Together, we learn better.

## License ##

This project is distributed under the GPL-3.0 License found in the [LICENSE](./LICENSE) file.
