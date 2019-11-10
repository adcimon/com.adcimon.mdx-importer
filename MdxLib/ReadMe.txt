+-------------------------------------------------------------------------------
| Info
+-------------------------------------------------------------------------------

¤ MdxLib v1.04
  Created by Magnus Ostberg (aka Magos)
  http://www.magosx.com


+-------------------------------------------------------------------------------
| What is this?
+-------------------------------------------------------------------------------

¤ MdxLib is a .NET 3.5 Class Library to handle WarCraft 3 models.
  It's mainly aimed for programmers to be used in tools and similar.


+-------------------------------------------------------------------------------
| Features
+-------------------------------------------------------------------------------

¤ An object oriented approach to handling and editing WarCraft 3 models.
  You handle objects and references, not raw data and ID's.

¤ Automation - if a component is removed from a model, all references to it
  are nullified instead of being invalid.

¤ Lazy initialization - some parts of the model are only created if neccessary.

¤ Buildt-in support for MDL, MDX and XML model formats.

¤ Buildt-in undo/redo system.

¤ Metadata - store your own custom data in the models (and still have them
  work properly in WarCraft 3).


+-------------------------------------------------------------------------------
| Notes
+-------------------------------------------------------------------------------

¤ When loading a model format always use a newly created CModel. Don't reuse
  a model that has been cleared, certain sideeffects may occur.

¤ Make sure when attaching vertices on a face and groups on a vertex that
  what you attach lies in the same geoset.

¤ Each geoset should have an amount of CGeosetExtent's equal to the number
  of sequences in the model.

¤ Use "Model.HasGeosets" rather than "Model.Geosets.Count > 0" since it
  retains laziness among the lazy initialization.

¤ When adding metadata do not use multibyte characters. These may corrupt
  the model if saved to MDX format. Stick to plain old ASCII.


+-------------------------------------------------------------------------------
| FAQ
+-------------------------------------------------------------------------------

¤ Q: There is no exe, how do I run it!?!?
  A: It's not an executable program, it's a .NET 3.5 Class Library.
     Unless you're a programmer you won't find this useful.

¤ Q: How do I use this library?
  A: Preferably you use Visual Studio with the 3.5 framework. Other environments
     *may* also be able to use it, but that's beyond my current knowledge.
     Just add a reference to the DLL and you can use the included classes.

¤ Q: I still don't know how to use it!
  A: There are some samples (example code) and there's a reference for use with
     intellisense (an external xml-file, just make sure it's in the same folder
     as the dll). Apart from this it's up to you. Writing a complete reference
     describing every aspect of mdx is way too much work. Sorry!

¤ Q: Can I have the source code for the dll?
  A: At the moment - no. I'm planning on releasing it as open source later, but
     I wan't it to be more complete and bugfree first.


+-------------------------------------------------------------------------------
| Version history
+-------------------------------------------------------------------------------

¤ 1.00 (2008-08-02)
  First version released. Everything is implemented as far as I know,
  however it requires lots of testing.

¤ 1.01 (2008-08-04)
  Added support for storing metadata in models. This way you can store whatever
  extra data you like without corrupting the model (some 3rd party tools may
  not work properly though).
  If you have problems opening the models in War3 Model Editor check out
  http://www.magosx.com for an updated version.

¤ 1.02 (2008-08-15)
  Various fixes. Made some classes and class constructors private
  (not accessible) since they should only be used internally anyway.

¤ 1.03 (2008-08-28)
  Completed the missing MDX animation tags through hex-hacking (no official
  blizzard models seems to use these)

  Light:
    KLAS - Attenuation Start
    KLAE - Attenuation End

  Particle emitter:
    KPEE - Emission Rate
    KPEG - Gravity
    KPLN - Longitude
    KPLT - Latitude
    KPEL - Life Span
    KPES - Initial Velocity

  Particle emitter 2:
    KP2R - Variation
    KP2G - Gravity

   Ribbon Emitter:
    KRCO - Color
    KRTX - TextureSlot

¤ 1.04 (2009-02-11)
  Model components can have tagged data (a Tag attribute) so you can attach
  custom data to them.
