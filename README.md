# SiteWatcher

<img src="https://github.com/hronoas/SiteWatcher/blob/main/manual/screen.jpg?raw=true" align="right" alt="SiteWatcher" width="300" /> SiteWatcher позволяет автоматически проверять и уведомлять об изменениях на страницах сайтов.

### Основные возможности программы:

* Неограниченное (ну почти) количество наблюдателей, для проверки интересующих страниц с определенными интервалами времени. 
* Уведомления об изменениях на отслеживаемых страницах.
* Подсветка изменений произошедших в содержимом страницы.
* Хранение истории произошедших изменений на странице.
* Авторизация на интересующих сайтах и проверка содержимого страниц, предназначенных для авторизованного пользователя.
* Отслеживание изменений конкретных элементов на страницах и обрабатка HTML-кода этих элементов с помощью регулярных выражений. 
* Группировка наблюдателей и фильтрация списка отображения.
* Режим работы из папки ([portable](#%D0%BD%D0%B0%D1%81%D1%82%D1%80%D0%BE%D0%B9%D0%BA%D0%B8-%D0%BE%D1%82%D1%81%D0%BB%D0%B5%D0%B6%D0%B8%D0%B2%D0%B0%D0%B5%D0%BC%D1%8B%D1%85-%D1%81%D0%B0%D0%B9%D1%82%D0%BE%D0%B2-%D1%85%D1%80%D0%B0%D0%BD%D1%8F%D1%82%D1%81%D1%8F-%D0%B2-%D1%84%D0%B0%D0%B9%D0%BB%D0%B5-watchesjson))

Скачать исходные тексты и скомпилированный вариант под Windows x64 можно в разделе [Releases](https://github.com/hronoas/SiteWatcher/releases/latest)

Описание элементов интерфейса приложения доступно по ссылке [SiteWatcher_GUI_desc.pdf](https://github.com/hronoas/SiteWatcher/raw/main/manual/SiteWatcher_GUI_desc.pdf)

### Минимальные системные требования для запуска

* Windows 7+ x64
* [.NET Framework 4](https://www.microsoft.com/download/details.aspx?id=17718)
* [.NET Desktop Runtime 6.0.6 x64](https://dotnet.microsoft.com/en-us/download)
* [C++ Redistributable for Visual Studio 2015 x64](https://learn.microsoft.com/ru-ru/cpp/windows/latest-supported-vc-redist)

### Настройки отслеживаемых сайтов хранятся в файле Watches.json

Watches.json после первого запуска находится в %appdata%\SiteWatcher

Если файл Watches.json находится в папке с программой, включается "portable" режим (необходимы права на запись в папку с программой)

### В проекте использованы сторонние библиотеки и код:

* [Notifications.Wpf.Core](https://github.com/mjuen/Notifications.Wpf.Core)
* [CalcBinding](https://github.com/Alex141/CalcBinding)
* [CefSharp](https://github.com/cefsharp/CefSharp)
* [Menees.Diffs](https://github.com/menees/Diff.Net)
* Копипаст из результатов поиска в интернете

### Порядок сборки из исходников:

1. Скачать исходники из репозитория
2. Установить библиотеки из минимальных требований
3. Запустить compile.cmd

В папке bin\Release будет находиться скомпилированный проект

### Дополнительная информация:

* При запуске программы, брандмауэр запрашивает разрешения. (Это связано с WebRTC через mDNS для защиты от утечки IP в CefSharp)
* CefSharp начиная с версии 106, при проверке изменений на страницах, начал определяться как Trojan.Win32.Generic в модуле "Анализ поведения" продуктов Касперского. (В этом случае необходимо добавить исключение для программы в настройках антивируса)

