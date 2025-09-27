# MIDI Piano Jukebox

My wife purchased a Baby Grand Player Piano, and much to my delight, it included MIDI Out and In connections.
Further, I inherited a PC from my father where he had accumulated over 170,000 MIDI files from varying genres.

Thus commenced my challenge: to create a MIDI Jukebox application to navigate through the thousands of MIDI files,
enable rating, remote management, and display any metadata that could help me sort and filter through them all.
I found a few small apps that were file and folder-based, but none that gave a comprehensive Jukebox experience
with customizable Playlists, a queue, and the ability to rate songs as they were played (some of the MIDI songs just don't fit my taste on the piano).

And since .NET released with the server-side Blazor capabilities, I knew the time was nigh to craft a Jukebox solution, where
I could run on an old laptop, and use a (now wireless via Bluetooth) USB MIDI adapter into the piano. The laptop serves as a dedicated web
server that runs on the Windows device connected to the Piano.

I first evaluated MIDI NuGet packages with LINQPad--to read through the MIDI files and iventory them into a LiteDB document database.
Then play the MIDI files through Windows' sequencer, and switchable to the Bluetooth MIDI adapter to the piano.

Here are the major technologies for the solution:
* .NET 10
* ASP.NET Core 10
* Blazor server-side to drive a server-connected MIDI device
* MudBlazor Material UI Components
* LiteDB Document Database
* Managed MIDI classes for MIDI file reading, parsing, and playing

## Setting up the site

The site is easy to setup. Follow these steps:
1. Use Visual Studio to build and publish the site to a folder on a Windows device (such as a small laptop) that acts as the "server".
2. The site runs as server-side Blazor, and, aside from playlist creation/management, is meant to "remotely control" a MIDI player that runs on the host
  server, which is connected via USB to the MIDI input ports on a player piano for example.
  You can use a direct-wire MIDI USB adapter between the host-server device and the MIDI device or a wireless Bluetooth MIDI connector,
  so the host-server doesn't have to be co-located with the piano.
3. Copy over your folder of MIDI files to the host-server device.
4. Once you get the site running (I prefer to run the site under IIS for its resiliency capabilities),
  open a browser to the site and click Settings (the gear button at the top-right), and set the path to the root folder of your MIDI files.
5. In Settings, click Refresh to perform the import process--scanning all the MIDI files for metadata used for playlist creation, etc.
  The Refresh process creates a server-side LiteDB database to persist all the data, your playlists, favorites, ratings, play counts, etc.
6. Also in Settings, choose the MIDI output device (which can be the 'Microsoft GS Wavetable Synth' device to synthesize the MIDI through the host-server speakers.)
  When you have a MIDI link to your piano, the MIDI device should show up in the Settings device drop-down; and when you select that for output, then the site will play MIDI files to the piano.
7. Use the Playlists dialog box to create and manage playlists.
8. Use the Home page to play the songs in the playlists.

You can remotely operate the Jukebox from most browsers from your phone, iPad, etc... I found the Blazor server-side application works well with Safari, Chrome, and Edge.
The app will track your song ratings, allow skipping forward, shufffle, play/pause, etc.

I've created playlists for "Ragtime", "Christmas", "Beatles", and plenty of other genres and keywords--even MIDI files recorded by certain artists that sounds great on the piano.
