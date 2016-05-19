BLE_UART_8001
======
A sample app like [nRF UART (iOS)](https://itunes.apple.com/us/app/nrf-uart/id614594903?mt=8) and [nRF UART 2.0 (Android)](https://play.google.com/store/apps/details?id=com.nordicsemi.nrfUARTv2&hl=en), but for Windows Phone 8.1

Connect your Windows Phone device via Bluetooth Low Energy (Bluetooth Smart) to a device running the custom Nordic Semiconductor UART service. Developed using the Arduino Uno with the Adafruit [Bluefruit LE - Bluetooth Low Energy (BLE 4.0) - nRF8001 Breakout - v1.0](https://www.adafruit.com/product/1697)

#### Screenshot


## Getting Started

### Installation

If you just want to try the app out, get it from the Windows Store!
[Download Link](https://www.microsoft.com/en-us/store/apps/smartpakku/9nblggh16nmk)

Otherwise, you probably want to actually use it for development. Open the solution (BLE_UART.sln) in Visual Studio and compile it with your Windows Phone device as the target.

As with the official nRF UART apps, I used the same UUIDs, which are:

6E400001-B5A3-F393-E0A9-E50E24DCCA9E
for the Service

6E400002-B5A3-F393-E0A9-E50E24DCCA9E
for the TX Characteristic Property = Notify

6E400003-B5A3-F393-E0A9-E50E24DCCA9E
for the RX Characteristic Property = Write without response



## Contributors

### Contributors on GitHub
* [Anthony Nguyen](https://github.com/resolutedreamer)

### Third party libraries
Heavily borrowed from official sample:
*  [Bluetooth Generic Attribute Profile - Heart Rate Service](https://code.msdn.microsoft.com/windowsapps/Bluetooth-Generic-5a99ef95)

## License 
* This project is licensed under the Apache License - see the [LICENSE.md](https://github.com/resolutedreamer/BLE_UART_8001/blob/master/LICENSE) file for details

## Version 
* Version 1.0

## Contact
#### Anthony Nguyen
* Homepage: www.resolutedreamer.com




Last Updated 2/23/2015
