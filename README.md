# Dobókocka szimulátor

Grafikus dobókocka-dobó program C#-ban (Windows Forms, .NET 8).

## Funkciók
- 1–10 kocka egyszerre, kirajzolt kockákkal és pöttyökkel
- Gurulás animáció (Dobás gomb, szóköz vagy kattintás a kockára)
- Az eredmények és az összeg kijelzése
- Előzmények listája és statisztika (melyik szám hányszor jött ki)

## Futtatás
Windowson, .NET 8 SDK-val:

```
dotnet run
```

## Önálló exe készítése
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```
A `publish/DobokockaApp.exe` telepítés nélkül futtatható.
