# Рядок формату Guid з суфікса введеного рядка.

<!--category-- C# -->
<datetime class="hidden">2024- 08- 07T17: 17</datetime>

## Малі повідомлення FTW

Маленьке, але потенційно корисне вирішення проблеми, яку я мала. Назвою, спосіб створення GUID з рядка вхідних даних, де Gid завжди є коректним, але унікальним для будь- якого вхідного рядка.

Мені це було потрібно для мого [Генератор подач RSS](/blog/addinganrssfilewithaspnetcore) де я хотів створити GUD для кожного елемента в подачі, який повторювався, але унікальний для кожного елемента.

Виявляється, що `XxHash128` є ідеальним для цього, оскільки воно завжди дає 128 бітів (або 16 байтів) хеш. Це означає, що його можна використовувати для створення GUD з рядка вхідних даних без " Array." Copy безглузда справа.

```csharp
     public  static string ToGuid(this string  name)
    {
        var buf = Encoding.UTF8.GetBytes(name);
        var guid = XxHash128.Hash(buf);
        var guidS =  string.Format("{0:X2}{1:X2}{2:X2}{3:X2}-{4:X2}{5:X2}-{6:X2}{7:X2}-{8:X2}{9:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}", 
            guid[0], guid[1], guid[2], guid[3], guid[4], guid[5], guid[6], guid[7], guid[8], guid[9], guid[10], guid[11], guid[12], guid[13], guid[14], guid[15]);
        return guidS.ToLowerInvariant();
    }
```

Це простий метод розширення, який отримує вхідні рядки і повертає GUID. The `XxHash128` походить з `System.IO.Hashing` Простір назв.

Ви, звичайно ж, можете використати будь-який алгоритм хешування, який дає 128 біт хеш. The `XxHash128` є просто хорошим вибором, як це швидко і дає хороший розподіл хешових цінностей.

Ви також можете повернути a `new Guid(<string>)` з цього, щоб отримати справжню Gid, яка може бути використана у базі даних або інших специфічних випадків використання GUD.