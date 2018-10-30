# gtautil

Generate archetype definitions (.ytyp)

```
gtautil genpropdefs --input 'path\to\*.ydr'
```


Import xml meta

```
gtautil importmeta --input 'path\to\file.ymap.xml' --directory 'output\directory'
```


Export meta to xml

```
gtautil exportmeta --iinput 'path\to\file.ytyp' --directory 'output\directory'
```

Extract ytyp mlo rooms to separate ymaps

```
gtautil extractentities --name '.\hei_dlc_heist_police' --ytyp '.\hei_dlc_heist_police.ytyp' --position 442.42960000,-985.06700000,29.88529000 --rotation 0,0,0,1
```

Note: It will create a folder named hei_dlc_heist_police and create one ymap per mlo room inside that folder

Inject ymap entities to mlo room

```
gtautil injectentities --name 'output\file\name.ytyp' --ymap '\path\to\*.ymap' --ytyp '\path\to\mlo.ytyp' --position 123.12,456.45,789.65 --rotation 0,0,0,1
```

Note: Output file has to be renamed to the same names as --ytyp parameter for it to work properly in game. I you use custom props you can use the --mods switch, it will load drawable definitions, like this :

```
gtautil injectentities --name 'output\file\name.ytyp' --ymap 'path\to\*.ymap' --ytyp '\path\to\mlo.ytyp' --position 123.12,456.45,789.65 --rotation 0,0,0,1 --mods 'path\to\*.ydr'
```


Find ymap / ytyp / mlo room

```
gtautil find --position 123.12,456.45,789.65
```

Rebuild cache from current GTAV installation

```
gtautil buildcache
```

Others specific commands

```
gtautil help
```

