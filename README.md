# Mi a legjobb megoldás kép megjelenítésére Bitmap forrásból WPF alkalmazásban?

## Bevezetés
Belekeveredtem egy nagyon egyszerűnek látszó problémába, ami aztán a részleteiben szép nagy ördögöt rejtett. Sikerült megtalálni az okot. A történet magában is tanulságos. Jól mutatja a .NET memóriakezelését, méghozzá több szinten és részletesen. És segítségével bepillanthatunk a teljesítményvizsgálat néhány hasznos eszközének a használatába (Visual Studio Diagnostic Tool, DotNetBenchmark, Red Gate ANTS Memory és Performance profiler). Úgy érzem ebbe a problémába más is belefuthat, vagy már belefutott és ennél akár jobb megoldást is talált.

Így megszületett ez a kódtár és ez a leírás.

A megtalált és kikísérletezett megoldás egyelőre megfelelel a céljainknak. Azonban már a lehetőségek mennyisége mutatja, hogy ez egy összetett téma. Így aztán, ha esetleg eszedbe jut bármi ötlet, hozzászólás, ne habozz és írdd meg, vagy küldj egy pull requestet.

És akkor a történet.

Néhány hete azzal a problémával szembesültem, hogy az ITFactory közvetítéséért felelős alkalmazása (a ScreenLight) túl sok processzoridőt fogyaszt. Néha random módon OutOfMemory kivételeket dob, pedig látszólag nem tölti ki a teljes memóriát. Ilyenkor a webkamera képe eltűnik. A notebook-omon (2x2GHz i7-es processzor, 8GB RAM, 200GB SSD) ez a jelenség egy-két perc után mindig előjön, aminek az első számú következménye rám nézve az, hogy még tesztelni sem tudom ezen a gépen az alkalmazást. Nos, a gép nem egy erőmű, de nem is kifejezetten gyenge futtatókörnyezet. Így ezzel a problémával foglalkozni kell. 

## Helyszínelés
Kiindulásként Visual Studioban elindítva alkalmazásunkat az látszik, hogy a GC (Garbage Collector, szemétgyűjtő) események gyakorlatilag folyamatosan történnek. Kis kitérő: mi is az a szemétgyűjtő?

### A Szemétgyűjtő (GC)
#### Menedzselt környezet
A .NET (hasonlóan a Java környezethez) menedzselt futtatókörnyezet. Ez egyszerűen fogalmazva azt jelenti, hogy a forráskódunk nem a processzor által közvetlenül feldolgozható utasítások sorozatára fordítódik le, hanem egy köztes nyelvre. Ez a nyelv az Intermediate Language (IL), vagy, mivel van köze a Microsoft-hoz, gyakran rövidítik így: MSIL. Ahhoz, hogy ezt a köztes nyelvű programot aztán futtatni lehessen, olyan környezetbe kell eljutatni, ami érti ezt a köztes nyelvet, és tovább alakítja a processzor által fogyasztható formára. Esetünkben a .NET futtatókörnyezet az ami az IL nyelvű programot futtatja. A célja ennek az egésznek, hogy terhet vegyen le a programozó válláról. Tehát egy csomó, nem kreatív, de nagy odafigyelést igénylő, precíz, adminisztrációs munkát, amit mindig el kell végezni. Egyszóval a piszkos munkát. Ennek érdekében bezárja egyfajta dobozba az alkalmazásokat, ami könnyebbé teszi felügyeletüket, így egyszerűbb megőrízni a rendszer biztonságát és stabilitását. További állandó feladat a **memóriakezelés**. Ha szükségünk van tárhelyre, akkor azt le kell foglalni, ha már nincs szükség rá, akkor fel kell szabadítani, és persze ezt az egészen érdemes karban is tartani, hogy a szabad hely ne ezer helyen szétszórva apró kis foltokban legyen megtalálható, hanem célszerűen egy nagy tengerben, ahonnan mindig nagyobb darabokat is le lehet lefoglalni. Hát ezt a memóriakezelést végzi el helyettünk a Garbage Collector, vagyis a szemétgyűjtő.

#### A Szemétgyűjtő működése 
(TODO)
STACK és HEAP
Példányosítás, referencia.
GC-Collect()

### Feltételezés
Visszatérve a problémánkhoz, az egyfolytában jelentkező GC eseményekhez. Logikus feltételezésnek tűnik, hogy valamilyen menedzselt osztály példányainak tömkelege készül, amiket aztán élettartama befejezésével a szemétgyűjtő szépen kitakarít. Ha elég munkát adunk a szemétgyűjtőnek, akkor nyilván simán rá lehet venni, hogy folyamatosan dolgozzon. És ha túl sok munkával látjuk el, akkor annyi takarítanivalót kap, amivel már nem bír el. Ha ilyenkor kér valaki memóriát tőle, tadam, ott az OutOfMemory kivétel.

Mivel a jelenség a program indulását követően minden egyéb funkció bekapcsolása nélkül előjön, feltételezhetően az egyetlen tevékenység, amire ez a leírás illik a kamera képének a megjelenítése. Ezt alátámasztja az is, hogy hiba esetén ez a kép eltűnik. A jó minőség érdekében nagy felbontású (1920x1080) képeket használunk és 20 FPS sebességgel (vagyis másodpercenként 20 új kép). ZHa számolunk, akkor itt mozognia kell másodpercenként 20x6=120MB-nak, percenként 60x20x6=7GB-nak. Mivel a Heap mérete alapértelmezésben (TODO), ez nem kevés. Tehát, ha szépen az egyes képeket létrehozzuk majd szinte azonnal meg is szüntetjük, akkor a szemétgyűjtőnek van mit takarítani, hogy ki ne szaladjunk a (TODO) -ból.

### Rövid ellenőrzés
Ha a képen látható módon bekapcsoljuk a CPU profiling lehetőséget, akkor láthatjuk, mivel tölti idejét a programunk. Itt az könnyen beazonosítható, bizony az egyes kameraképek megjelenítésével dolgozik nem keveset. Nyomon vagyunk. De mi ilyen nehéz? Hogyan lehet egyáltalán egy WPF programban képet megjeleníteni?

## Kép megjelenítése WPF-ben
A WPF programban kép megjelenítésére az [System.Windows.Controls.**Image** vezérlőt](https://msdn.microsoft.com/en-us/library/system.windows.controls.image) használjuk. Ez egy meglehetősen intelligens eszköz, ha beállítjuk a [*Source*](https://msdn.microsoft.com/en-us/library/system.windows.controls.image.source) tulajdonságát, ahol egy [System.Windows.Media.**ImageSource**](https://msdn.microsoft.com/en-us/library/system.windows.media.imagesource) típust vár, akkor minden további nélkül megjeleníti a képet. XAML-ben megadhatunk neki például egy helyi file-t így:

```CSharp
<Image Width="200" Source="Images/myImage.png"/>
```
Ha kódból szeretnénk a forrást megadni, akkor általában a [System.Windows .Media.Imaging.**BitmapImage**](https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.bitmapimage) osztályt használunk. Mi sem egyszerűbb. Nos, a kameráról érkező kép egy [System.Drawing.**Bitmap**](https://msdn.microsoft.com/en-us/library/system.drawing.bitmap(v=vs.110).aspx) osztály. Ahhoz, hogy megjelenítsük, át kell alakítani BitmapSource típussá. A névterekből gyanús, hogy egyszerű típuskonverzióval nem fog menni. És itt kezdődik az érdekes kihívás. Ugyanis nekünk nem egyszerűen átalakítanunk kell, hanem *másodpercenként 20 alkalommal kell* ilyen átalakítást végeznünk, úgy, hogy *közben szemétgyűjtőt erőn felül ne terheljük*.

Értelmes feladatnak látszik tehát összeszedni a lehetséges algoritmusokat és megmérni őket. Egyrészt mérni azt, hogy mennyi processzoridőt használnak, másrészt mérni azt, hogy mennyire terhelik a szemétgyűjtőt.  

## Teljesítménymérés
Teljesítményméréshez a [BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet) csomagot fogjuk használni. Ez egy nyílt forráskódú könyvtár, többek között [John Skeet](https://codeblog.jonskeet.uk/) és [Matt Warren](http://mattwarren.org/) keze munkáját dícséri.

(TODO: Nugetek)

### Projektek
Ahhoz, hogy használjuk, készítünk két projektet egy solution-ban, az egyik fogja az algoritmusokat tartalmazni (BitmapToImageSource). Mivel csak az algoritmusokat mérjük, ezért egy sima konzol alkalmazás meg fog felelni. A másik projekt szintén egyszerű konzol alkalmazás a teljesítménytesztekhez (BitmapToImageSource.Benchmarks). Ahhoz, hogy a teszteket futtassuk, a második projektet jelöljük ki alapértelmezetté (Jobb egérgomb, és *Set as StartUp* project menüpont).
![](images/step1.png?raw=true)

### BenchmarkDotNet nuget telepítése

A .Benchmarks projektbe telepítsük a [BenchmarkDotNet nuget](https://www.nuget.org/packages/BenchmarkDotNet/)-et. Ezt két féle módon tehetjük meg.

#### Telepítés Nuget Package Manager segítségével
Betöltjük a Nuget package manager ablakot (jobb egérgomb a *Solution* soron a *Soution explorer* ablakban, majd *Manage Nuget Packages for Solution...* menüpont, vagy *Tools\Nuget Package Manager\Manage Nuget Packages for Solution...* menüpont) és a következő módon járunk el:

![](images/step2.png?raw=true)
 1. Az ablakban kiválasztjuk a *Browse* menüpontot,
![](images/step3.png?raw=true)
 1. A keresőmezőbe beírjuk a könyvtár nevét (benchmarkdotnet),
![](images/step4.png?raw=true)
 1. Kiválasztjuk az első csomagot, ekkor megjelennek a jobb oldali ablakban a projektek, ahova telepíthetjük,
![](images/step5.png?raw=true)
 1. Kiválasztjuk a benchmark projektünket, ahva telepíteni szerenénk, majd
![](images/step6.png?raw=true)
 1. Install gombra kattintunk.

#### Telepítés Package Manager Console segítségével
A Package Manager Console ablakot vagy a *View\Other Windows\Package Manager Console* menüpont segítségével, vagy a *Tools\Nuget Package Manager\Package Manager Console* menüpont segítségével érjük el. Fontos, hogy a Default projekt lenyílóban a benchmark projektünk legyen kiválasztva:
![](images/step7.png?raw=true)

Itt aztán telepítjük a következő sorral a csomagot:

PM> **Install-Package BenchmarkDotNet**

Ez a parancs a BenchmarkDotNet csomag függőségeit felderíti és telepít mindent: 

```
PM> Install-Package BenchmarkDotNet
Attempting to gather dependency information for package 'BenchmarkDotNet.0.9.8' with respect to project 'BitmapToImageSource.Benchmarks', targeting '.NETFramework,Version=v4.5.2'
Attempting to resolve dependencies for package 'BenchmarkDotNet.0.9.8' with DependencyBehavior 'Lowest'
Resolving actions to install package 'BenchmarkDotNet.0.9.8'
Resolved actions to install package 'BenchmarkDotNet.0.9.8'
Adding package 'Microsoft.CodeAnalysis.Analyzers.1.1.0' to folder 

(... itt egy csomó telepítés történik ...)

'C:\Users\Gábor\BitmapToImageSource\BitmapToImageSource\packages'
Added package 'BenchmarkDotNet.0.9.8' to 'packages.config'
Successfully installed 'BenchmarkDotNet 0.9.8' to BitmapToImageSource.Benchmarks
PM> 
```

A nuget telepítésének eredményeképpen megjelent a referenciák között a BenchmarkDotNet assembly:
![](images/step8.png?raw=true)

### Tesztek
Most, hogy ezzel megvagyunk, készítünk egy osztályt, ami a teljesítményteszteket futtatja. Ehhez szükségünk lesz a **Bitmap** osztálytípusra, ezért a referenciák közé vegyük fel a System.Drawing assembly-t (jobb egérgomb a *References* soron, majd *Add reference...* menüpont):
![](images/step9.png?raw=true)

Itt megkeressük és bepipáljuk a System.Drawing assemblít és OK-t nyomunk, ezzel felvettük a megfelelő referenciát:

![](images/step10.png?raw=true)

Szóval van Bitmap típusunk, ezért képesek vagyunk a következő tesztelésre:

 1. Létrehozunk egy kezdeti Bitmap osztályt,
 1. Konvertáljuk ImageSource típusra
 1. és közben mérjük a teljesítményt.

Ehhez a következő osztályt hozzuk létre:

```CSharp
using BenchmarkDotNet.Attributes;
using System.Drawing;

namespace BitmapToImageSource.Benchmarks
{
    public class BitmapToImageSourceTests
    {
        Bitmap bitmap;

        [Setup]
        public void SetupData()
        {
            bitmap = new Bitmap(1920, 1080);
        }

        [Benchmark]
        public void TestBitmapToImageSource1()
        {

        }
    }
}
```

Megjegyzések:

1. Figyeljünk a megfelelő névterek betöltésére
1. Létrehozunk egy osztályszintű változót, amiben a kiindulási Bitmap példányunk referenciáját tároljuk
1. Készítünk egy SetupData függvényt, ami előállítja minden teszt előtt a kezdeti állapotot. A Bitmap mérete a programunk által használt méret. Ezt a függvényt a BenchmarkDotNet minden teszt futtatása előtt meghívja, erről a **Setup** attributum gondoskodik.
1. Minden teszthez készítünk egy függvényt, ami a tényleges munkát elvégzi. A **Benchmark** attributum gondoskodik arról, hogy a BenchmarDotNet számára láthatóak legyenek az egyes tesztfüggvények  

## Lehetséges algoritmusok
Összeszedtem a lehetséges algoritmusokat, amiket csak találtam ehhez az átalakításhoz.

