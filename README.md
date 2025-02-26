# Unified Host but Better

**Unified Host but Better** is a C# desktop application designed for flashing multiple PIC microcontrollers (MCUs) over an Ethernet connection using a UDP-based Over-The-Air (OTA) update protocol. The application reads and parses a hex file, then transmits it to multiple MCUs through an OTA sequence of commands adjusted to a preprogrammed bootloader.

## Features

- **Hex File Parsing**: Reads and interprets standard hex files for firmware updates.
- **UDP-based OTA Updates**: Sends firmware updates via UDP following a structured OTA command sequence.
- **Multi-Board Broadcasting**: Utilizes async functions and threading to broadcast updates to multiple MCUs simultaneously.
- **Per-Board Logging**: Maintains separate logs for each board to track the update process.
- **Custom Port Selection**: Allows users to specify the UDP port for communication.

## How It Works

1. **Load the Hex File**  
   The application reads and processes the provided hex file.

2. **Select the Communication Port**  
   Choose the UDP port for transmitting data to the MCUs.

3. **Initialize Ethernet Communication**  
   Establishes a UDP connection to the target MCUs.

4. **OTA Firmware Transmission**  
   The firmware is uploaded using a sequence of OTA commands specific to the preprogrammed bootloader.

5. **Multi-Board Handling**  
   The update is broadcasted to multiple MCUs using asynchronous operations and threading.

6. **Logging & Status Updates**  
   Logs are maintained individually for each board to track the progress and status of the firmware update.

## Usage

1. Launch the application.
2. Select the hex file for firmware flashing.
3. Configure the target devices and specify the UDP port.
4. Initiate the OTA update process.
5. Monitor the logs for update status.
