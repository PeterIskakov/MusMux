# MusMux
MusMux (Music Multiplexer) is a music player which replaces playlists and shuffling with a prompt dialog for what you want next. You get 6 random picks and you click whichever you want to hear now.

<img width="1504" height="1080" alt="MusMux with selector" src="https://github.com/user-attachments/assets/18f5ae96-cdb8-43d9-b411-d723ba558037" />

## Why?
I made MusMux because I found choosing my songs generally just more enjoyable than getting a random song on shuffle. Most apps make you navigate a massive list just to pick a song, while MusMux gives you a nice and small popup.  

## Features
- "Next song" popup at the end of each song, with a refresh button in case you don't like the options  
- Metadata reading using TagLib# when importing songs
- Persistent song library across sessions
- A modern WinUI 3 interface

## Building
MusMux was built with Visual Studio 2026, specifically with .NET 8 and WinUI 3. To build, simply open the project and run it as a packaged app.  

MusMux does depend on certain packaged features for saving your songs, so it cannot be used as unpackaged. It relies on packaged app data APIs which simply are not available for an unpackaged app.

## Other
MusMux makes use of TagLib#, see THIRD-PARTY-NOTICES.txt for more info.
