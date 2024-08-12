# 来自字符串输入扩展名的指导格式字符串 。

<!--category-- C# -->
<datetime class="hidden">2024-008-007T17:17</datetime>

## 小型员额

用于解决我遇到的一个问题的小型但可能有用的解决方案。 也就是说, 如何从字符串输入中生成一个 GUID, 其中“ Guid” 总是有效的, 但对任何给定的输入字符串来说是独一无二的 。

我需要这个[RSS feed 发电机](/blog/addinganrssfilewithaspnetcore)我想为种子中的每个项目生成一个 GUID, 它是可重复的, 但每个项目都独一无二 。

事实证明`XxHash128`这对于它来说是完美的, 因为它总是给出128 位( 或 16 位元) hash 。 这意味着它可以用来从没有“ Array” 的字符串输入中生成 GUID 。 compy 无稽之谈 。

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

这是一个简单的扩展方法, 使用字符串输入, 返回一个 GUID 。 The`XxHash128`是来自`System.IO.Hashing`命名空间 。

当然,你可以使用任何能给出128位散列的散列算法。`XxHash128`这只是一个好选择,因为它是快速的, 并给出一个良好的散列值分布。

您也可以返回`new Guid(<string>)`从这里获得可用于数据库或其他图形用户界面数据系统具体使用案例的实际《指南》。