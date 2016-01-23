# Introduction #
Seamless branching is a feature found on Blu-ray, HD DVD and DVD titles that allows multiple versions of a video to be placed efficiently on a single disc. Take the Blu-ray of the movie **Salt**, which includes the theatrical, extended, and director's cut. Or the Blu-rays of the TV series **Star Trek: TOS**, which includes a version with remastered effects and one with the original effects. In both of these examples, the versions differ only in a limited number of scenes; the majority of the video can be reused for the additional versions.

Xin1Generator attempts to replicate this behavior in the context of the commonly used Matroska media container. It does this in the following way: take one version of the video, and append all the _different_ scenes of the additional versions to the end. Then use Matroska's ordered chapters to navigate around this video, and selectively hide scenes that don't belong to the version you're watching. If this sounds weird to you, that's because it is. But it's a completely legitimate use of Matroska's advanced features, and allows us to efficiently store all of these versions (called editions in Matroska) in a single file.

# Requirements #
  * A Windows PC with Microsoft's .NET Framework 4 (only Client Profile required). You _should_ be able to compile Xin1Generator's command line version with [Mono](http://www.mono-project.com/) on other operating systems, but this is currently untested and unsupported.
  * [eac3to](http://forum.doom9.org/showthread.php?t=125966). Either extract this to a folder and add that folder to your [PATH](http://en.wikipedia.org/wiki/PATH_%28variable%29), or extract eac3to to the same folder you extracted Xin1Generator to.
  * [xport](http://www.w6rz.net/). Already included with Xin1Generator releases because it's small and not updated anymore.

# Interface #
You can run Xin1Generator as a command line program or a GUI program. For the purpose of this guide, we'll only discuss the GUI. Advanced users can map the GUI options to command line options.

The first thing you'll want to do is set the **Input path**. This should point to the _root_ of the disc. eac3to may be smart enough to recognize BDMV or HVDVD\_TS folders, but Xin1Generator isn't. The **Output path** should be set to location with a sufficient amount of space. "Sufficient" in this case means "roughly the size of the original disc".

You'll notice that as soon as you set your input path, a couple of titles will appear under **Available titles**. The titles represent playlists: the multiple versions of the video, plus any additional features on the disc, such as a _making of_. Unfortunately, for Blu-rays, there isn't really a good way to determine which title represents which version other than looking at the length. If this proves problematic, you'll have to use eac3to to find out which version uses which video files, and watch these video files to associate titles with versions. Select the titles you want and click the **Add** button for each of them in order of importance.

This will cause the **Selected titles** list to fill up. You can use this list to name your editions. If you happen to have stumbled upon a seamlessly branched HD DVD, you're in luck; these already include title names. Having at least one selected title will also fill up the **Tracks** list. Select the tracks you want to extract by ticking the check boxes in front of them. You can specify the extension, which means you can tell eac3to to automatically encode your audio to FLAC by specifying "flac", or extract the core DTS track by specifying "dts -core". Alternatively, leave all tracks unchecked to use eac3to's -demux mode instead.

Finally, there's the two check boxes above the tracks list. Enabling **Extract tracks** will do exactly what it says: extract the tracks. The unchecked state is slightly less obvious. This will, instead of executing the extraction command right away, write these commands to a file, allowing you to extract the tracks at a later time.

**Preserve chapters** requires some additional explanation. As described in the introduction, Xin1Generator uses chapters to replicate seamless branching. If you asked yourself "So what happens to the regular chapters?" you're on the right track. The short answer is that you can't simply use the regular chapters as well. Enabling the preserve chapters feature, however, will read the regular chapters that are present on the disc, and mix them in with the hidden generated chapters. This would be incredibly annoying to do manually.

Once you've set everything up correctly the **Start** button will be enabled. Clicking this will generate all the files you need in the output path. This process might take a long time.

# Output #
Xin1Generator will output a couple of files:
  * **chapters.xml**: The file that makes everything work. Add this as the **Chapter file** under mkvmerge GUI's **Global** tab.
  * **tags.xml**: Specifies the names of the editions. Not strictly needed, but it looks nicer. Add this as the **Tag file** under mkvmerge GUI's **Global** tab.
  * **qpfile.txt**: Only useful if you'll be re-encoding the video with x264. This file forces I-frame placement on chapter borders, making sure that playback is seamless. Reference this file with x264's **--qpfile** option.

And depending on whether you selected the extract tracks option, you'll either already have your extracted tracks, or:
  * **extract.cmd**: Extracts all the tracks you selected in the GUI. Feel free to customize the command in this file.

You may re-encode or OCR the tracks. When you're done, you'll want to mux everything together in a single Matroska file. This is most commonly done with mkvmerge GUI.

# Playback #
Support for Matroska's ordered chapters exists, but is currently limited to PCs. The best method to play these files is to use [LAV Splitter](https://code.google.com/p/lavfilters/) (recommended), [Haali's Media Splitter](http://haali.su/mkv/) or [AV Splitter](http://avsplitter.avmedia.su/en) together with a sane media player such as [Media Player Classic Home Cinema](http://mpc-hc.sourceforge.net/). If you're looking for a cross-platform solution, [mplayer2](http://www.mplayer2.org/) works. [VLC](http://www.videolan.org/vlc/) might as well. Hardware support is pretty much non-existent, unfortunately.

One thing to note is that the generated files aren't _entirely_ useless if your player does not support ordered chapters. Your player will still fully play the first edition of your file (with some additional scenes at the end that you can easily ignore). This is why it may be important to think about the order in which you want to place the titles under Xin1Generator's selected titles list; the first one might be the only one you can watch on some of your less advanced players.