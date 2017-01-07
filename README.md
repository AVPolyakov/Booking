# Комплементарный граф для Composition Root
При использовании Dependency Injection требуется составить граф объектов. 
Это известно под названием 
[Composition Root](http://blog.ploeh.dk/2011/07/28/CompositionRoot/). 
В [статье](http://blog.ploeh.dk/2012/11/06/WhentouseaDIContainer/) 
Mark Seemann описал три варианта построения Composition Root.

Характерной чертой графа объектов является многоуровневая вложенная структура. 
Для создания такой вложенной структуры можно прибегнуть к множеству методов(*1), 
которое подобно графу объектов имеет вложенную структуру. Множество методов
образует комплементарный граф.

На схеме из [статьи](http://blog.ploeh.dk/2012/11/06/WhentouseaDIContainer/) 
комплементарный граф располагается в левом верхнем углу:  
<img src="Images/usefulness-sophistication.png?raw=true" width="661"/>

Таблица сравнения:

| DI style               | Advantages    | Disadvantages     |
| -----------------------|---------------| ------------------|
| Poor Man's DI          | Easy to learn <br/>Strongly typed | High maintenance |
| Explicit Register      |                                   | Weakly typed     |
| Convention over Configuration | Low maintenance | Hard to learn<br/>Weakly typed |
| Complementary Graph | Easy to learn <br/>Static typed<br/>Very low maintenance<br/>Symbol-based navigation<br/>Easy refactoring<br/>Error checking at compile time |  |

Для демонстрации комплементарного графа был сделан fork репозитария Mark Seemann:
["Demo code demonstrating how to manage a complex code base with DI, Convention over Configuration, etc"](https://github.com/ploeh/Booking). 
Комплементарный граф находится в 
[строках](BookingDaemon/Program.cs#L20-L44).
При переходе с Convention over Configuration на Complementary Graph
[количество строк кода уменьшилось на 134](https://github.com/AVPolyakov/Booking/commit/60b3b171545a5cb547e674fbd2223620140df923).

При работе с комплементарным графом полезной является команда ReSharper 
[Smart Completion](https://www.jetbrains.com/help/resharper/2016.3/Coding_Assistance__Code_Completion__Smart.html).

## Ссылки, которые заставляют думать
1. После того как [стали писать](http://www.davidarno.org/2013/11/13/are-ioc-containers-a-case-of-the-emperors-new-clothes/)
, что король [голый](https://ru.wikipedia.org/wiki/%D0%9D%D0%BE%D0%B2%D0%BE%D0%B5_%D0%BF%D0%BB%D0%B0%D1%82%D1%8C%D0%B5_%D0%BA%D0%BE%D1%80%D0%BE%D0%BB%D1%8F).
Mark Seemann [в 2014 году признал](http://blog.ploeh.dk/2014/06/10/pure-di/), 
что сделал ошибку в своей книге. 
[Poor man’s DI is dead; long live Pure DI.](http://www.davidarno.org/2015/10/12/poor-mans-di-is-dead-long-live-pure-di/)
2. Джоэл Спольски, основатель сайта Stack Overflow: 
[I believe that if you use IoC containers, your code becomes, frankly, a lot harder to read. And somewhere in heaven an angel cries out.](http://stackoverflow.com/questions/871405/why-do-i-need-an-ioc-container-as-opposed-to-straightforward-di-code/871420#871420)
3. Дядя Боб: [I think Dependency Inversion is so important that I want to invert the dependencies on IoC!](https://sites.google.com/site/unclebobconsultingllc/blogs-by-robert-martin/dependency-injection-inversion)
4. Сокрытие информации просто стоит у меня на пути, когда мне нужно понять код и модифицировать его:
[Hiding that information away in auto-wiring frameworks ... just gets in my way when I later need to understand the code and modify it.](http://www.natpryce.com/articles/000783.html)

(*1) Кроме методов могут использоваться свойства и другие элементы языка.