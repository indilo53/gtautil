# gtautil

Generate archetype definitionss (.ytyp)

```
gtautil genpropdefs --input path/to/*.ydr
```


Import xml meta

```
gtautil importmeta --input path/to/file.ymap.xml --directory output/directory
```


Export meta to xml

```
gtautil exportmeta --iinput path/to/file.ytyp --directory output/directory
```


Inject ymap entities to mlo room

```
gtautil injectentities --name "/output/file/name.ytyp" --ymap /path/to/file.ymap --ytyp /path/to/mlo.ytyp --room RoomName --position 123.12,456.45,789.65 --rotation 0,0,0,1
```

Note: Output file has to be renamed to the same names as --ytyp parameter for it to work properly in game.


Find ymap / ytyp / mlo room

```
gtautil find --position 123.12,456.45,789.65
```
