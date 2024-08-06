# DynamicWin

<p align="center">
  <img src="https://i0.wp.com/sixcolors.com/wp-content/uploads/2022/09/di-animated-clip.gif?ssl=1" alt="animated" />
</p> // Replace this gif lol

### What is it?
A [Dynamic Island](https://support.apple.com/de-de/guide/iphone/iph28f50d10d/ios) inspired Windows App that brings in a bunch of features like widgets or a file tray that works like a clipboard.

### Why did I make this?
The idea for this application originally came to me when I saw the dynamic island on iPhone for the first time. After seeing that there are no (good) solutions to a Dynamic Island type application on Windows, I got the idea to make my own.
I only got the motivation to start on it though, after seeing something similar has been done on macOS already [(NotchNook)](https://lo.cafe/notchnook).
<br><br>
I love programming and I had the idea stuck in my mind for quite some time now. <br>
I originally made the project in [Java](https://www.java.com/de/) (luckily, that changed) but didn't get any progress since Java is less connected to the Operating system than a language like C#. <br>
I re-made the project in [WinForms](https://de.wikipedia.org/wiki/Windows_Forms) getting way better results and being more motivated and then had to migrate it to  [WPF](https://de.wikipedia.org/wiki/Windows_Presentation_Foundation) because of my stupid mistake of not trying if things would actually work before doing it. (:D)

### How did I make this?
WPF is a powerful UI framework, however to archive the look and feel of this app I decided on creating every UI element from scratch using [SkiaSharp](https://github.com/mono/SkiaSharp) for the rendering. This allowed me to create an app that looks like something you would find on macOS which was perfect for this project.

### Why am I writing all of this instead of talking about the features?
I wanted to give you a peak at what went into this project and why/how I made it.

# Features
> [!NOTE]
> Only the checked features are currently available.

DynamicWin has a variety of features, currently including: <br>

## Shortcuts
- [x] ``Ctrl + Win`` Will hide the island (or show it again).
- [ ] ``Shift + Win`` Will open a quick search menu.

## Big Widgets
- [x] Media Playback Widget
- [x] Timer Widget
- [ ] Weather Widget
- [ ] Voicemeeter implementation Widget
- [ ] "Shortcuts" Widget <sub>(Can be configured to a certain action like opening an app or link)</sub>
- [ ] Calendar Widget
- [ ] Tuya Smart integration <sub>(Will probably be turned in to one widget with the shortcuts)</sub>

## Small Widgets
- [x] Time Display
- [x] Music Visualizer
- [x] Device Usage Detector <sub>(Indicates if camera / microphone is in use)</sub>
- [x] Power State Display <sub>(Shows battery in form of icons. If no battery is found it shows a connector icon instead)</sub>
- [x] Timer <sub>(Displaying current running timer)</sub>

## File Tray <br>
Files can be dragged over the island to add them to the file tray. The tray can be accessed when hovering over the island and clicking on the 'Tray' button. The files are stored until they are dragged out again. They can also be removed by selecting the file and right clicking. A context menu will popup and you can click on - **"Remove Selected Files"** or **"Remove Selected Files"** to copy the files. <br>
Idea for the future: An implementation of a service like [SnapDrop](https://snapdrop.net) to allow for an "AirDrop" kind of feature using the file tray.

# Known Issues
The performance might not be the best on older hardware or laptops. I will try my best to add performance options to the settings menu but cannot guarantee a smooth experience for everyone. <br><br>

The app might suddenly disappear and upon reopening it a message box will tell you that only one instance of the app can run at the same time. To fix this, open task manager and find the process "DynamicWin". Kill it and start the app again. <br><br>

Too fast interactions might confuse the animation system and will result in an empty menu. To fix this, usually moving the mouse away from the island and then over it again will fix it.