
# ПАРСЕР ДЛЯ ФАЙЛОВ-РЕЕСТРОВ ПРИВАТБАНКА

(Dmitry, dima_ben@ukr.net)
(написано: 2023-08-10; доработка: 2023-09-19)

## Суть:

В выписке из "приватбанка" довольно много операций группируются и помечаются как 
"Оплата згiдно реєстру № @xxxxxxxxx вiд nnnnnnnn"
И там внутри этого "реестра" может быть много платежей.
При проведении ревизии ОСББ очень хочется иметь детали по всем таким реестрам - кто платил, когда, сколько.
Поэтому появилась эта программа - "ReestrParser".
Эти файлы как-то можно скачать (я сам не скачивал, мне выдали уже скачанные).
Вроде они приходят на email председателю ОСББ...


## Техническая часть 

Исходим из того, что мы втягиваем все "*.txt" файлы из указанной директории...

Пример заголовка из файла:

------------------------------------------------------------------------------------------------------------------------------------------   
              | Опер |     Ф.И.О.      |  Лицевой  |       Адрес/          |  Счетчики  |    Периоды     |   Принятая   |   Комис.         
    № док.    | день |  плательщика    |   счет    |       Телефон         |н.зн. -к.зн.|    оплаты      |    сумма     |      с           
              |      |                 |           |                       |            |                |              |   получ.         
------------------------------------------------------------------------------------------------------------------------------------------   


Нужна некая база данных для хранения результата...

Но дело в том, что программа должна быть свободно используемой и не требовать установки. 
Поэтому, видимо, DB2 не подойдет (DB2 у нас на виртуалках с основным продуктом - мне проще ее использовать).

Или же нужно сохранять DataTable в XML а потом открывать XML с помощью AdoNetQuery - тоже вариант...
Хотя у нас по сути две таблицы (или даже 3) - а моя тулзовина AdoNetQuery в один момент времени работает с одной таблицей...

Ну тогда пусть будет - SQLite, она файловая, однопользовательская и не требует установки...

Программа работы с файлами SQLite - SQLite Browser: https://sqlitebrowser.org/dl/
Она легкая и бесплатная.
Также в ней можно выполнять различные SQL запросы к базе данных - для ревизии это полезно.
А результаты запроса из SQLite Browser можно напрямую вставить в Excel...

Видимо это должна быть не консольная версия, а иначе - как люди будут эти данные вытягивать?...
Т.е. нужен какой-то примитивный UI - выбрать директорию и кнопка [Run]...

Прикинем структуру таблицы:

    create table "Reestr" (
      docNo        char(40)     not null,
      operDay      char(10)     not null,
      name         char(100)    not null,
      account      char(40)     not null,
      address      char(100)    not null,
      counters     char(100)    not null,
      payinterval  char(40)     not null,
      amount       float        not null,
      commission   float        not null,
    )

Хотя лучше так...

    CREATE TABLE "ReestrItem" (
      reestrId     INTEGER  not null,
      idx          INTEGER  not null,
      docNo        TEXT     not null,
      operDay      TEXT     not null,
      name         TEXT     not null,
      account      TEXT     not null,
      address      TEXT     not null,
      kvRef        TEXT     not null default '',
      counters     TEXT     not null,
      payinterval  TEXT     not null,
      amount       REAL     not null,
      commission   REAL     not null,
      primary key (reestrId, idx)
    );

__Примечание__: kvRef - это ссылка на номер квартиры - специфично для проведения ревизии...

И там есть еще шапка в каждом файле...

    CREATE TABLE "ReestrHeader" (
      id               INTEGER  not null UNIQUE,
      filename         TEXT     not null,
      lang             TEXT     not null,
      date             TEXT     not null,
      sndBank          TEXT     not null,
      sndCode          TEXT     not null,
      sndAccount       TEXT     not null,
      rcvBank          TEXT     not null,
      rcvCode          TEXT     not null,
      rcvAccount       TEXT     not null,
      reestrName       TEXT     not null,
      reestrDate       TEXT     not null,
      po               TEXT     not null,
      poDate           TEXT     not null,
      totalAccNo       TEXT     not null,
      totalItems       INTEGER  not null,
      totalCommission  REAL     not null,
      totalSum         REAL     not null,
      totalConfirmed   REAL     not null,
      primary key (id)
    );

Будем считать, что первая версия не имеет правильных primary key - у нас все что распарсилось, будет вставляться в базу без проблем.
Т.е. дубликаты реестров - отслеживайте сами. Это не сложно. 
Например, при ревизии я нашел, что было два продублированных реестра с разными именами файлов и один реестр был потерян - в выписке он есть, а файла к нему нет...

__Примечание__: в подпапке "db\empty" храниться исходный пустой файл базы данных.



## Исходный код программы

После 4 попыток нашел работающий вариант - как работать с C# проектом из VsCode:
(у меня VS2012 - из нее мне привычней, но раз уж выкладываю в массы, то нужно рассчитывать на публичные инструменты)

    dotnet new console --name ReestrParser --target-framework-override net472
    dotnet add package XService.Net2
    dotnet add package System.Configuration.ConfigurationManager
    dotnet add package System.Data.Sqlite


Забавная вещь - Visual Studio 2022 не понимает вот такую секцию в *.csproj файле - из UI ее никак не видно:

    <PropertyGroup>
      <RunArguments>D:\archive\OSBB-revision-2023\РЕЕСТРЫ\*.txt</RunArguments>
    </PropertyGroup>

Довольно глупый баг. 

    
Сделал параллельный проект для отладки парсера в VS2012, пока не понял как настроить отладчик в VsCode,
нет времени с этим возиться...

Добавил код для использования #define UseDB - чтобы можно было отлаживать сам парсер без SQLite библиотек.
Поскольку с совместимостью пакетов там страшный ужас!...


Похоже там должен быть двухтипный парсер:
  * для разбора заголовка и окончания - классический синтаксический
  * для разбора данных в таблице - там нужно позиционно вырезать нужные значения

На удивление это оказалось несложно - я могу вырезать нужный кусок для позиционной резки...


Все... данные сохраняются в SQLite db...

Но вот в Excel - с этим пока сложности, почему-то сам Excel не хочет втаскивать мой HTML файл.
Видимо чего-то ему не хватает чтобы воспринимать HTML с таблицей как Excel документ, хотя раньше похожий метод у меня срабатывал...
Пока оно сохраняет HTML в output.xlsx - можно просто переименовать в *.HTML и открыть в браузере...
Нет времени разбираться, проведение ревизии и создание инструментов для нее не является моей основаной работой, поэтому здесь нужно уложиться в минимальное время...


Добавил параллельную обработку, т.е. чтобы сам парсинг нагружал CPU по полной. 
Но эффект заметно сказываться только если парсим 2.000 файлов и больше. Там реально в 4-5 раз быстрее выходит.
А у нас только 400 файлов реестра, поэтому - вообще никак не заметно, даже на 1 секунду медленнее...


Сделал UI-mode и чтобы он включался по умолчанию. При этом окно консоли остается на заднем плане и туда идет вывод текущих операций...


Там есть валидация результатов!
Т.е. после парсинга оно сверяет по каждому файлу - а совпадает ли сумма по операциям с итоговой суммой по этому реестру.


## Настройки

Вот такой текст выводится если запускаем с ключиком /? или --help

    Usage: ReestrParser filespecs [...filespecs] [options]

    Supported options:
      -?  - print this message
      -cd, --change-dir={directory}  - switch directory when started
      -dbc, --db-cleanup[=1|0]  - perform db cleanup before inserting data; default is 1
      -o, --output={format}  - add output format, one of: db, excel (default output is 'db')
            you can specify {format} with '-' prefix to exclude it
      -p, --pause={list}  - set pause params, comma-separated list of: error, begin, end, always
      -r, --recursive[=1|0]  - recursive scan; default is 1
      -ui=[1|0]   - run in UI-mode; default is 1

    Examples:
      0. Run normally (UI mode)
        ReestrParser

      1. Run from command line (and disable UI-mode)
        ReestrParser -ui:0 c:\bank\private\data\*.txt


В *.exe.config файле можно прописать параметры командной строки по умолчанию:

    <appSettings>
      <add key="CLI:Input" value="D:\archive\OSBB-revision-2023\reestry\*.txt" />
      <add key="CLI:UI" value="1" />
      <add key="CLI:Pause" value="end" />
      <add key="CLI:Output" value="-db,excel" />
    </appSettings>

Все указанное в *.exe.config это "стартовые настройки", параметры командной строки их перезаписывают...

__Примечание__: *.txt можно не указывать оно по умолчанию ищет *.txt файлы


## Тесты

Тесты показывают, что парсинг файлов занимает около 2-5 секунд.
А вот запись в базу данных занимают ощутимо больше времени 
- до минуты если база находится на HDD-диске
- до 20 секунд если база на SSD-диске

Это на 400 файлах, 1450 платежей.



СЦЕНАРИЙ ИСПОЛЬЗОВАНИЯ
=======================

Сохраняем файлы реестра где-то в одной папке (с любым количеством под-папок).

Запускаем ReestrParser.exe ...

- Можно запустить из командной строки: 
    - ReestrParser.exe -ui:0 C:\temp\reestry

- Можно запустить с UI и там уже выбирать нужные параметры в UI
    - ReestrParser.exe 


