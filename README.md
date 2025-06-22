# DynamicWin

<p align="center">
  <img src="https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white">
  <a href="https://creativecommons.org/licenses/by-sa/4.0/"><img src="https://img.shields.io/static/v1?label=License&message=CC+BY-SA+4.0&color=%23c49b04&style=for-the-badge"></a>
  <a href="https://discord.gg/UHFuqB9NqR"><img src="https://dcbadge.limes.pink/api/server/https://discord.gg/UHFuqB9NqR)](https://discord.gg/UHFuqB9NqR"></a>
</p>

<p align="center">
  <img src="ReadmeFiles/IslandGif-1_Volume.gif" style="border-radius:15px" alt="animated" width="1000" height="auto" />
</p>

<p xmlns:cc="http://creativecommons.org/ns#" xmlns:dct="http://purl.org/dc/terms/"><a property="dct:title" rel="cc:attributionURL" href="https://github.com/FlorianButz/DynamicWin">DynamicWin</a> by <a rel="cc:attributionURL dct:creator" property="cc:attributionName" href="https://github.com/FlorianButz">Florian Butz</a> is licensed under <a href="https://creativecommons.org/licenses/by-sa/4.0/?ref=chooser-v1" target="_blank" rel="license noopener noreferrer" style="display:inline-block;">CC BY-SA 4.0<img style="height:22px!important;margin-left:3px;vertical-align:text-bottom;" src="https://mirrors.creativecommons.org/presskit/icons/cc.svg?ref=chooser-v1" alt=""><img style="height:22px!important;margin-left:3px;vertical-align:text-bottom;" src="https://mirrors.creativecommons.org/presskit/icons/by.svg?ref=chooser-v1" alt=""><img style="height:22px!important;margin-left:3px;vertical-align:text-bottom;" src="https://mirrors.creativecommons.org/presskit/icons/sa.svg?ref=chooser-v1" alt=""></a></p>

> [!NOTE]
> This App is undergoing a major re-write from scratch, which will hopefully soon be released.

### What is it?
A [Dynamic Island](https://support.apple.com/de-de/guide/iphone/iph28f50d10d/ios) inspired Windows App that brings in a bunch of features like widgets or a file tray that works like a clipboard.
Similar to dynamic notches that you can find on macOS like [NotchNook](https://lo.cafe/notchnook), this application brings the concept on Windows devices to life.

### Implementation and build
This application is developed using C# for the logic, and [SkiaSharp](https://github.com/mono/SkiaSharp) to display the graphical interface.
To build this project, ensure that you have the latest version of **.NET 8.0** installed on your environment.

To get started:
```bash
git pull https://github.com/59xa/DynamicWin.git
```

### Future plans
If you want to support the development of this project you can create pull requests and contribute to it.<br>
Feel free to open up a new issue if you encounter any bugs/flaws.
<br>

**Quick disclaimer**: The codebase is currently structured terribly and almost un-maintainable. A possible re-write of the entire codebase may happen some time in the future.

# Features
> [!NOTE]
> Only the checked features are currently available. Unimplemented features will be introduced as time passes.

DynamicWin has a variety of features, currently including: <br>

## Shortcuts
- [x] `Ctrl + Win` Will hide the island (or show it again).
- [ ] ~~`Shift + Win` Will open a quick search menu.~~ <sub>(Please consider using an alternative such as [Powertoys Run](https://learn.microsoft.com/en-us/windows/powertoys/run))</sub>

## Big Widgets
- [x] Media Playback Widget
- [x] Timer Widget
- [x] Weather Widget
- [x] Shortcuts Widget <sub>(Can be configured to open a file, e.g. Shortcut, .EXE or any other filetype.)</sub>
- [ ] Calendar Widget

## Small Widgets
- [x] Time Display
- [x] Music Visualizer
- [x] Device Usage Detector <sub>(Indicates if camera / microphone is in use)</sub>
- [x] Power State Display <sub>(Shows battery in form of icons. If no battery is found it shows a connector icon instead)</sub>
- [x] Timer <sub>(Displaying current running timer)</sub>
- [x] CPU/GPU Usage Display

## File Tray <br>

<p align="center">
  <img style="border-radius:15px" src="ReadmeFiles/IslandGif-2_Tray.gif" alt="animated" width="1000" height="auto" />
</p>

Files can be dragged over the island to add them to the file tray. The tray can be accessed when hovering over the island and clicking on the 'Tray' button. The files are stored until they are dragged out again. They can also be removed by selecting the file and right clicking. A context menu will popup and you can click on - **"Remove Selected Files"** or **"Remove Selected Files"** to copy the files. <br>
Idea for the future: An implementation of a service like [SnapDrop](https://snapdrop.net) to allow for an "AirDrop" kind of feature using the file tray.

> [!WARNING]
> If you are using the file tray to import files in to an app (e.g. After Effects) make sure to not remove the files from the tray. Apps that only copy a link to the file will loose it after you remove the file from the tray.

## Spotify Integration

<p><img align="left" height="150" style="border-radius:15px; margin:0 25 0 0" src="ReadmeFiles/IslandGif-3_Spotify.gif">The Media Playback Widget automatically detects when an instance of the Spotify app is running (Desktop version only). It will display the current playing song name and the artist. Login to the Spotify service on the app is <b>not</b> required.</p>
<br><br><br>

## Mod Support
**Yes! You can add your own small widgets and big widgets by creating a custom extension!** <br>
Loading an extension from someone else is very simple. You just need to drag the **Mod.dll** file in to the *Extensions* folder that is located in the `%appdata%/DynamicWin` directory. 

> [!WARNING]
> **Please never load a mod that is not tested to be safe!**

Mods can contain malicious code that can mess up your system, so always check a mod's source code or let a trustworthy person check it for you.

## Custom Themes

<p align="center">
  <img style="border-radius:15px; margin:15px 0 15px 0" src="ReadmeFiles/Themes.png" alt="animated" width="100%" height="auto" />
</p>

> [!NOTE]
> Custom themes are not the main priority for this repository, but will remain supported for use. Visit Florian's Discord server to get access to more themes like the ones shown from above.

You can use the built-in dark / light theme. You can also create custom themes that fit your liking by going to the `%appdata%/DynamicWin/Theme.json` file. After editing the colors you need to select the `Custom` theme option in the settings. If you already did that, you will need to go back to the settings and click on it again. Otherwise you would have to restart the app.
<br>
This is an example of a color:
`"IslandColor": "#000000"`
<br>
The hex code is structured this way: `#rrggbb`. If you want to change the alpha of the color, it is **always** at the start of the code. `#aarrggbb`.

# Known Issues
The performance might not be the best, as said by Florian. More optimisations in the codebase will happen as time passes. <br><br>

The app might suddenly disappear and upon trying to reopen it a message box will tell you that only one instance of the app can run at the same time. To fix this, open task manager and find the process "DynamicWin". Kill it and start the app again. <br><br>

Too fast interactions might confuse the animation system and will result in an empty menu. To fix this, usually moving the mouse away from the island and then over it again will fix it.

# Creating an Extension
To create an extension you need an IDE like [Visual Studio 2022](https://visualstudio.microsoft.com/de/vs/community/).
- Create a new C# project of the type "Class Library". Ensure that the target framework is `.NET 8.0`.
- You are required to add `DynamicWin.dll` and SkiaSharp DLLs as assembly dependencies to your project. [More information regarding this through here.](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-add-or-remove-references-by-using-the-reference-manager?view=vs-2022)
- Create a new C# class file, if it's not already there. Rename the class to something like "MyExtension".
- All extensions must have a class that implements the `IDynamicWinExtension` interface.<br>

Here is an example of how the extension class should look like:

```cs
public class TestExtension : IDynamicWinExtension
{
    public string AuthorName => "Florian Butz"; // The display name of the author (you) of the extension

    public string ExtensionName => "Test Extension"; // The dislpay name of the extension

    public string ExtensionID => "florianbutz.test"; // The ID of the extension

    public List<IRegisterableWidget> GetExtensionWidgets() // Returns all Widgets that are available in this extension
    {
        return new List<IRegisterableWidget>() { }; // Creates a list with all IRegisterableWidgets in this extension
    }

    public void LoadExtension() // Gets executed after DynamicWin has finished loading in all extensions and has created the window
    {
        System.Diagnostics.Debug.WriteLine(ExtensionName + " was loaded sucessfully!"); // Debug text
    }
}
```

Now you can create your first widget. Create a new class and call it how you want. I will call mine `TestWidget` for now. The class has to implement from `WidgetBase` if it is a big widget. If you want to make a small widget, extend the class from `SmallWidgetBase`.

```cs
public class TestWidget : WidgetBase
{
    DWText text; // Reference for the text object

    public TestWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment) // Overriding the constructor is essential.
    {
        text = new DWText(this, "This is a Text!", Vec2.zero, UIAlignment.Center); // Creates a new text object
        AddLocalObject(text); // Adds an object to the UIObject (Widget)
    }

    public override void DrawWidget(SKCanvas canvas) // Gets called when the widget is drawn. Do not override 'Draw()' since it contains other important things.
    {
        var paint = GetPaint(); // Creates a new paint of the UIObject. Please only use this and don't create your own paint.
        paint.Color = Theme.Primary.Value(); // Sets the color of the paint to the current Primary color

        var rect = GetRect(); // Gets the rect / bounds of the widget

        canvas.DrawRoundRect(rect, paint); // Draws the rect to the screen. Only use 'DrawRoundRect()' when trying to draw the bounds of the UIObject.
    }
}
```

DynamicWin needs to know what widgets are in your extension. To register the widget, create a new class that implements from the `IRegisterableWidget` class. For readability reasons I'll call mine `RegisterTestWidget`.

```cs
public class RegisterTestWidget : IRegisterableWidget
{
    public bool IsSmallWidget => false; // Determins if the widget is supposed to be small or big (small widgets are displyed when the island is not hovered)

    public string WidgetName => "Test Widget"; // The display name of the widget

    public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) // Overriding the constructor is essential.
    {
        return new TestWidget(parent, position, alignment); // Needs to return the Widget that you are trying to register
    }
}
```

Now the extension is almost ready. As a last step, go back to your main extension class and add the `RegisterTestWidget` (or however you called it) class to the `GetExtensionWidgets()` function's return list.

```cs
public List<IRegisterableWidget> GetExtensionWidgets() // Returns all Widgets that are available in this extension
{
    return new List<IRegisterableWidget>() { new RegisterTestWidget() }; // Creates a list with all IRegisterableWidgets in this extension
}
```

Now you're done. Build the project and go to your project's output folder. (Most times located under `\bin\Debug\net8.0\` or `\bin\Release\net8.0\` and move **ONLY** the DLL file that has the name of your project in to the `%appdata%/DynamicWin/Extensions` folder. In this case, my output DLL is called `TestExtension.dll`. <br><br>
After that you can run DynamicWin and test your extension. This was of course a very bare bones example.

> [!TIP]
> It's best to look at the widgets that are already in DynamicWin and learn from them.

<br>
Here is a small list of current UIObjects that can be used to make the widget creation process easier.

## UIObject
  - Constructor
    - `UIObject? parent` is the parent UIObject, most times the widget itself.
    - `Vec2 position` is the position of the UIObject.
    - `Vec2 size` is the size of the UIObject.
    - `UIAlignment alignment` is an optional paramenter which determins where inside the parent the object's zero point is.
  - Important Methods
    - `SetActive(bool)` sets the active state of the UIObject with an animation.
    - `SilentSetActive(bool)` sets the active state of the UIObject without an animation.
    - `SKRoundRect GetRect()` returns the bounds of the UIObject (`SKRoundRect`);
    - `ContextMenu? CreateContextMenu()` is overriden if the UIObject is supposed to have a context (right click) menu using the WPF `ContextMenu` class.
    - `AddLocalObject(UIObject)` adds another UIObject inside the UIObject that called it. Parent is automatically set to the caller UIObject.
    - `DestroyLocalObject(UIObject obj)` removes the local UIObject.
    - `GetColor(Col)` takes in a color and returns the same color but with correct transparency. Please use it whenever you use custom colors or colors from the Theme class.
  - Important Fields / Properties
    - `IsHovering` returns true if the mouse is over the UIObject.
    - `IsMouseDown` returns true if the mouse is down over the UIObject. 
    - `Color` returns the color of the object.
    - `Position` returns the position of the object.
    - `RawPosition` returns the actual position of the object without the screen transformation (use this when setting the `Position` field).
    - `Anchor` is the anchor of the object. By default set to `(0.5f, 0.5f)` which means zero is in the middle of the UIObject.
    - `Size` returns the size of the object.
    - `IsEnabled` returns the active state of the UIObject.

## DWText
  - Constructor
    - `string text` is the text.
  - Important Methods
    - `SetText(string)` sets the text with an animation.
    - `SilentSetText(string)` sets the text without an animation.
  - Important Fields / Properties
    - `Font` is the Typeface of the text.
    - `TextBounds` are the bounds of the text.
    - `TextSize` is the size of the text.

## WTextImageButton
(Do not use. This is the base class of `DWTextButton`, `DWImageButton` and `DWTextImageButton`)
  - Constructor
    - `Action clickCallback` is an action that gets called on click.
  - Important Fields / Properties
    - `normalColor` is the color of the button when not hovered.
    - `hoverColor` is the color of the button when hovered.
    - `clickColor` is the color of the button when clicked.
    - `colorSmoothingSpeed` is the speed at which the color updates.
    - `normalScaleMulti` is the size of the button when not hovered.
    - `hoverScaleMulti` is the size of the button when hovered.
    - `normclickScaleMultialScaleMulti` is the size of the button when clicked.


## DWTextButton
  - Constructor
    - `string buttonText` is the text on the button.
  - Important Fields / Properties
    - `normalTextSize` is the size of the text when not hovered.

## DWImageButton
  - Constructor
    - `SKBitmap image` is the image on the button.
  - Important Fields / Properties
    - `imageScale` the scale multiplier of the image. Default is `0.85f`. `1f` would be the scale of the button.
    - `Image` the image of the button.
    
## DWTextImageButton
  - Constructor
    - `SKBitmap image` is the text on the button.
    - `string buttonText` is the text on the button.
    - `Action clickCallback` is an action that gets called on click.
  - Important Fields / Properties
    - `imageScale` the scale multiplier of the image. Default is `0.85f`. `1f` would be the scale of the button.
    - `normalTextSize` is the size of the text when not hovered.
    - `Image` the image of the button.

The `Res` class can be used to load in SKTypeface or SKBitmap (for DWText or DWImage).
For colors the `Col` class is used. When applying the color to the paint `Col.Value()` has to be called to convert to `SKColor`. <br>
Try to only use the colors available in the `Theme` class to provide color customizability to the user.
