# SiteWatcher

Программа для автоматической проверки изменений на страницах сайтов

Описание элементов интерфейса можно посмотреть в [SiteWatcher_GUI_desc.pdf](https://github.com/hronoas/SiteWatcher/raw/main/manual/SiteWatcher_GUI_desc.pdf)

## Как пользоваться:

### Настройки отслеживаемых сайтов хранятся в файле Watches.json

Watches.json после первого запуска находится в %appdata%\SiteWatcher

Если файл Watches.json находится в папке с программой, включается "portable" режим (нужны права на запись в папку с программой)

### В проекте использованы сторонние библиотеки и код:

* [Notifications.Wpf.Core](https://github.com/mjuen/Notifications.Wpf.Core)
* [CalcBinding](https://github.com/Alex141/CalcBinding)
* [CefSharp](https://github.com/cefsharp/CefSharp)
* [Menees.Diffs](https://github.com/menees/Diff.Net)
* Копипаст из результатов поиска

### Минимальные требования:

* Windows 7+ x64
* .Net Framework 4
* .NET Desktop Runtime 6.0.6 x64
* C++ Redistributable for Visual Studio

### Порядок сборки из исходников:

1. Скачать исходники из репозирория
2. Установить библиотеки из минимальных требований
3. Запустить compile.cmd

В папке bin\Release будет находиться скомпилированный проект

### Дополнительная информация:

CefSharp начиная с версии 106, начал определяться как Trojan.Win32.Generic
