
(?) Предполагаемая среда исполнения - Windows + .Net Framework или Windows + Linux + контейнеры + .Net Core ?
Windows + .Net Framework/Core 

(?) Нужен ли Fault tolerance на ошибки дисковой системы? Программа должна делать несколько попыток перечитать блок, который не считался с первого раза? Или программа может просто падать в таких случаях, предполагая, что её перезапустит оркестратор с той же задачей? 
Ошибку чтения можно считать критической для процесса обработки, успешное завершение работы алгоритма в этом случае не ожидается 

(?) С дисковой системой предполагается работать в один поток или считывать блоки нужно тоже параллельно? 

Эффективным для наиболее общего случая образом 

(?) Допускается ли использовать стандартные concurrent-коллекции и/или примитивы синхронизации типа ManualResetEventSlim, CountdownEvent? Допускается 

(?) Есть ли ограничения на размер блока, например не менее 16 байт и не более 256^2 - 1? 

Ограничение только размерностью Int32, т.е. 2^31 - 1. Специальной обработки для блоков малой длины не ожидается - если есть какие-либо внешние ограничения (например в работе встроенной в .Net реализации SHA256), то их выделенная обработка не требуется. 

(?) Есть ли формат для вывода сигнатуры или её можно выводить в свободной форме? Например так: "{номер блока} {хеш}" "{номер блока} {хеш}" и т.д. 

Можно в свободной форме, приведённый формат вполне подходит 

(?) Нужно ли выводить хеш последнего блока (если он меньше заданного размера блока)? 

(?) Нужно ли выводить хеш последнего блока нулевой длины? Например если размер файла ровно 100 KБ, а размер блока 1 КБ? 

Сигнатуры разных файлов ожидаются разными (с точностью до коллизий вычисления хэша), одинаковых - одинаковыми, вне зависимости от места в котором находятся различающиеся байты

(?) Возможна ситуация, когда блок заданного размера превышает размер доступной оперативной памяти?
Например, если задан размер блока Int32.MaxValue, то доступная оперативная память будет 4+ GB. Или это не обязательно?

Специальной обработки для ситуации, когда размер блока первышает объём оперативной памяти не требуется. Можно расчитывать на то, что доступен объём памяти минимально необходимый для эффективной работы алгоритма.
