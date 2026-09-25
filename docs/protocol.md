# Nari Ultimate receiver reports

These notes come from the [USB captures in the razer-nari-driver repository](https://github.com/felixZmn/razer-nari-driver/tree/main/pcapng). They are observations of Synapse traffic to a Nari Ultimate receiver (`1532:051A`), not a complete protocol specification.

The settings interface is USB interface 5, HID collection 3 on Windows. Its feature report is 64 bytes. The report ID is `FF`, and Synapse sends it with a HID `SET_REPORT` request (`21 09 FF 03 05 00 40 00` in the captures). The first five report bytes are `FF 0A 00 FF 04`. All remaining bytes after the command are zero.

| Setting | Report bytes, excluding zero padding | Evidence |
| --- | --- | --- |
| HyperSense off, strength 100 | `FF 0A 00 FF 04 02 F1 06 20 00 64` | `haptic feedback on 20 to 100 off.pcapng` |
| HyperSense on, strength 20 | `FF 0A 00 FF 04 02 F1 06 20 01 14` | same capture |
| HyperSense on, strength 30…90 | same report; last byte `1E`…`5A` | same capture |
| Static lighting off | `FF 0A 00 FF 04 12 F1 03 71 00` | `static led off.pcapng` |
| Static lighting on | `FF 0A 00 FF 04 12 F1 03 71 FF` | `static led on.pcapng` |

The capture does not show a color selection report. OpenNari currently exposes only the verified lighting on/off command. The HyperSense strength byte appears to be a direct percent, but values outside the captured 20–100 range still need hardware verification. The app uses 20–100 as its selectable range.

`GET_REPORT` replies in the captures do not clearly contain the current settings, so the app does not claim to know the headset's state when it opens. It shows only settings sent in the current session.
