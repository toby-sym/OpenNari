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
| Static color RGB | `FF 0A 00 FF 04 12 F1 05 72 RR GG BB` | [OpenRGB Nari Ultimate issue capture](https://gitlab.com/CalcProgrammer1/OpenRGB/-/issues/2114) |

The OpenRGB capture contains changing RGB triples in the command above. OpenNari applies a steady color by sending one report after turning the lights on. A connected headset confirmed that an `FF0000` color command turns the earcups red. Other colors should be checked on hardware.

The HyperSense strength byte appears to be a direct percent. The captures cover 20–100; a connected headset confirmed that sending the on flag with intensity 0 makes the haptics quiet. The app allows 0–100.

`GET_REPORT` replies in the captures do not clearly contain the current settings, so the app does not claim to know the headset's state when it opens. It shows only settings sent in the current session.
