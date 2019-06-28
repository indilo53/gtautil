# GTAUtil



------

## Credits

Neodymium146 for his work on RageLib

Dexyfex for Codewalker

_____



If you have any question or want to share an improvement, you can join me on discord https://discord.gg/z63cmAe

You must have basic knowledge with command-line to use this tool properly

It is recommended to add GTAUtil folder to your %PATH% for quick access

**Extract an rpf archive**

```powershell
gtautil extractarchive --input path\to\archive.rpf --output path\to\outputfolder
```



**Create an rpf archive from a folder, sub-folders with name ending with .rpf will be converted to an rpf archive in the generated rpf**

```powershell
gtautil createarchive --input path\to\inputfolder --output path\to\outputfolder --name dlc
```



**Encrypt an rpf archive**

```powershell
gtautil fixarchive --input path\to\archive.rpf --recursive
```

**Compile gxt2 file data for given language**

```powershell
gtautil compilegxt2 --lang american --output path\to\outputfolder
```



**Extract ytyp mlo rooms to separate ymaps**

```powershell
gtautil extractentities --name '.\hei_dlc_heist_police' --ytyp '.\hei_dlc_heist_police.ytyp' --position 442.42960000,-985.06700000,29.88529000 --rotation 0,0,0,1
```

It will create a folder named hei_dlc_heist_police and create one ymap per mlo room inside that folder. If there is more than one MLO archetype in the ytyp, you can specify it with --mloname



**Inject back ymap entities to ytyp mlo room (this is the opposite of the previous operation)**

```powershell
gtautil injectentities --name 'output\file\name.ytyp' --ymap '\path\to\*.ymap' --ytyp '\path\to\mlo.ytyp' --position 123.12,456.45,789.65 --rotation 0,0,0,1
```

Note: Output file has to be renamed to the same names as --ytyp parameter for it to work properly in game. I you use custom props you can use the --mods switch, it will load drawable definitions, like this :

```powershell
gtautil injectentities --name 'output\file\name.ytyp' --ymap 'path\to\*.ymap' --ytyp '\path\to\mlo.ytyp' --position 123.12,456.45,789.65 --rotation 0,0,0,1 --mods 'path\to\*.ydr'
```

You can also specify MLO name with --mloname



**Generate archetype definitions (.ytyp)**

```powershell
gtautil genpropdefs --input 'path\to\*.ydr'
```

If you want to keep infos from existing ytyp:

```powershell
gtautil genpropdefs --input 'path\to\*.ydr' --ytyp 'path\to\*.ytyp'
```


**Generate ped definition files for mp freemode including ymt** (addon clothing and ped props)

1) Creating the project (selecting only the base ped definition package)

```powershell
gtautil genpeddefs --create --output pedproject --targets mp_m_freemode_01,mp_f_freemode_01
```

mp_m_freemode_01 folder is for ped components and mp_m_freemode_01_p folder is for ped props, same for female.



2) Place your ydds in the correct project directory following this simple pattern : 0.ydd, 1.ydd, 2.ydd etc...

Start with 0 for every different component / prop

For the textures, in the ydd folders create folders 0, 1, 2 etc... Inside these : 0.ytd, 1.ytd etc...

See example below :

![example](https://i.ibb.co/c17skKQ/mini.png)



3) Create the rpf archive structure, reserving 200 drawables for each component and prop

```powershell
gtautil genpeddefs --input pedproject --output build --reserve 200 --reserveprops 200
```

Or Create a FiveM resource

```powershell
gtautil genpeddefs --input pedproject --output build --reserve 200 --reserveprops 200 --fivem
```

4) If you are building a normal dlc (for singleplayer or ragemp for example) you have to build the rpf archive from the folder structure

```powershell
gtautil createarchive --input build --output . --name dlc
```

if will generate a dlc named **gtauclothes** (hardcoded for now) so you put the dlc.rpf in **dlcpacks/gtauclothes**

**Generate collision from drawable**

1) Generate static collision

```powershell
gtautil gencol --input mfile.ydr --output mfile.ybn
```

2) Generate embedded collision

```powershell
gtautil gencol --input mfile.ydr --output file2.ydr
```

3) Generate static collision using convex hull algorithm

```powershell
gtautil gencol --input file.ydr --output file.ybn --mode hull
```

4) Various options

```powershell
gtautil gencol --input file.ydr --output file.ybn --mode hull --smooth 10 --triangles 200
```

smooth : Number of passes to smooth out mesh

triangles: Max mesh triangles count



**Import xml meta**

```powershell
gtautil importmeta --input 'path\to\file.ymap.xml' --directory 'output\directory'
```



**Export meta to xml**

```powershell
gtautil exportmeta --input 'path\to\file.ytyp' --directory 'output\directory'
```



**Find ymap / ytyp / mlo room**

```powershell
gtautil find --position 123.12,456.45,789.65
```



**Rebuild cache from current GTAV installation** - Do it after a game update, this will take some time

```powershell
gtautil buildcache
```



**Remove entity including LOD parenting**

```
gtautil exportmeta --input 'path\to\file.ymap' --directory 'output\directory' --metadata
# Remove one or more entities from the ymap
gtautil importmeta --input 'path\to\file.ymap.xml' --directory 'output\directory' --metadata
```



**Remove LOD lights given position and radius**

1) Create project

```bash
gtautil genlodlights --create -o path/to/directory
```

2) Remove LOD lights from project (modified files will be in **path/to/directory/modified** folder)

```powershell
gtautil genlodlights --delete -i path/to/directory --position 0,0,0 --radius 200
```



**Others specific commands**

```
gtautil help
```

```
gtautil appletname help
```

