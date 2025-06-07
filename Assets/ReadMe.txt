----------------------Lukma----------------------

----Prerequisite for Lukma---
Requires Microsoft .NET Runtime (minimum of 6.0)
Windows will prompt you to install it. Manually can be found here: https://dotnet.microsoft.com/en-us/download/dotnet
MacOS will be provided with Lukma or can be found manually above. Manually can be found here: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.31-macos-x64-installer



----After Install----
1.	Make sure videos are being placed where you want them. From the top menu, go to ‘Configure->Settings’ to change this. 
2.	Updated/Change logos. Go to ‘Configure->Logos’ from the top menu
3.  MacOS only: go into Settings->Privacy & Security under full disk or files & folders enable Lukma


----Notes/Descriptions----

---Manually editing any config files is unsupported and will have unexpected consequences---
New license.ini files goes in the config directory. XPlatformLukma --> Contents --> MacOS --> Config. It should be in there after installation.

Preserve Category/Name checkbox - After you search for a video, it will use the last Category/Name that was last used. Great for events, single team, or single videographer use.

Eject Media After Save - Tries to eject the media after you hit save. It does check to verify the drive type before ejecting. if it's not usb, it won't eject it.

Trimming videos - Trim start must be before trim end. Both Start and End trim have to be defined. If they aren't, the video will not be processes.

Settings dialog – Unconverted videos is where it will copy the video to before it gets converted. Teams will get their own directory. All other categories will be put in directory structure based on the date. Converted videos directory is the top level directory of where all converted videos get stored. Private teams get a separate directory. Teams gets a separate directory. All other categories go into another directory. From that top level directory ‘Teams’ category goes into ‘teamuploadvideos’ directory. From that top level directory ‘Teams - Private’ category goes into ‘privateteamuploadvideos’ directory. Allother categories go to an ‘uploadvideos’ directory. Bitrate is defaulted to low. Comparatively Low is about half the file size of High. Hardware acceleration option (uses graphics card to convert video). Defualt is on, if you see errors with conversion, uncheck it.

Categories and Names dialog – ‘Fun Jumper’ category is the only protected category. ‘Teams’ and ‘Teams – Private’ are handled differently but are not protected. All other categories are handled the same. If you create a new category, make sure to add a name in that category.

Logo sizes are based on original video size. This is only noticeable if you change your camera settings or when multiple people are filming in different resolutions
Logos dialog – This dialog allows you to change the standard logo on every video or add a secondary logo to a single team, private or regular team, or all other categories. New logos should not be any larger that 200x200 pixels in order to look correct on a 1080p video. The Current logo picture will show you what logo is being applied to whatever is selected on the left side. New logo gives you a preview of a logo that is selected after a search.
	‘Primary’ – the primary logo that is used for all videos. One must exist otherwise the video will not be converted.
	‘Category’ – All other categories except ‘Teams’ and ‘Private – Teams’
	‘Teams’ – applies to a single team
	‘Private – Teams’ - applies to a single team

	How to applying a new logo – On the left hand side, select what you want the new logo to apply to. Hit search, find and select the png file. It will show up in the New logo preview section. Hit apply

Score the Dive – Timer can be adjusted up or down. If the timer has been manually adjusted and then used. Reset will revert it back to whatever it has been adjusted to. This is not saved, so if the program is closed and reopened, it will go back to 35 seconds.

Questions or Issues, see the 'About' from the Configure->About menu


----Known Issues----
Yeah! None currently


----Version Information----
----1.9.0---- coming soon!
Added New Eject Media after save option

----1.8.3----
Fixed issue: Lukma locking up after loading searching for a video

----1.8.2----
Added default Category Events. They are handled just like teams


----1.8.1----
Search remembers where you last looked for a video
Code refactoring for memory improvements
Multiple instance of application are not allowed
Fixed video rotation problem when video is not recorded normally(i.e. gopro is upside down)
Added hardware acceleration and option in Settings menu (default is on)
New option to perserve category and name dropdowns ()
Fixed: Maximizing Lukma can cause video playback issues
Fixed: added empty folder cleanup to the file cleanup

----1.7.0----
Improved error messages when a logo can't be found

----1.6.0----
Added unconverted video folder cleanup enhancement
Removed primary logo requirement

----1.5.0----
-Bug fix with Trim
-Only allowing 1 instance of lukma to be open at a time
-Force popup dialogs to be closed before moving on in Lukma
-Add an option in Settings for lower or higher bitrate (reduces the file size by half at lower bitrate)default is lower bitrate

----1.4.0----
-Bug fix with logos dialog and added error messaging

