# MIDI Piano Jukebox

My wife purchased a Baby Grand Player Piano, and much to my delight, it included MIDI Out and In connections.
Further, I inherited a PC from my father where he had accumulated over 170,000 MIDI files from varying genres.

Thus commenced my challenge: to find (or create) a MIDI Jukebox application to navigate through the thousands of MIDI files,
enable rating, remote management, and display any metadata that could help me sort and filter through them all. I found a few small apps that were file
and folder-based, but none that gave a comprehensive Jukebox experience with customizable Playlists, a queue, and the ability to
rate songs as they were played (some of the MIDI songs just didn't fit my taste for the Piano).

And since .NET Core 3 and Blazor were just released, I knew the time was nigh to craft a Jukebox web site myself, where
I could run on an old Windows 10 laptop with IIS, and use a USB MIDI adapter into the piano. This is a dedicated web
server that runs on the Windows device hard-connected to the Piano.

I first evaluated MIDI NuGet packages with LINQPad--to read through the MIDI files and iventory them into a LiteDB document database.
Then ultimately to Play the MIDI files through Windows' sequencer, and switchable to the USB MIDI adapter to the Piano.

Here are the major technologies for the solution:
* .NET 8
* ASP.NET Core 8
* Blazor (server-side)
* MudBlazor Material UI Components
* LiteDB Document Database
    