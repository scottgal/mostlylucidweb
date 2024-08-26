# مُدِّر المواضيع للمُحْرَمَة

<!--category-- Mermaid, Markdown, Javascript -->
<datetime class="hidden">2024-08-26-TT20: 36</datetime>

## أولاً

أستخدم حورية البحر لإنشاء الرسوم البيانية الرائعة التي ترونها في عدد قليل من الوظائف. مثل واحد تحت.
لكن الشيء الذي أزعجني هو أنه لم يكن رد فعل على تغيير المواضيع (الظلام والضوء) وبدا أن هناك معلومات ضعيفة جدا هناك عن تحقيق هذا.

هذه هي نتيجة بضع ساعات من الحفر ومحاولة لمعرفة كيفية القيام بذلك.

[رابعاً -

## الـ

```mermaid
sequenceDiagram
    participant Window as window
    participant Mermaid as mermaid
    participant Document as document
    participant LocalStorage as localStorage

    Window->>Init: initMermaid()

    Init->>SaveOriginalData: Call saveOriginalData()
    SaveOriginalData->>Document: querySelectorAll(elementCode)
    SaveOriginalData->>Element: Set 'data-original-code' for each element
    SaveOriginalData->>Init: Resolve saveOriginalData promise

    Init->>Document: Add event listener for 'dark-theme-set'
    Init->>Document: Add event listener for 'light-theme-set'

    Note over Init: Event Listener for 'dark-theme-set'
    Document->>ResetProcessed: Trigger resetProcessed() on dark-theme-set
    ResetProcessed->>Document: querySelectorAll(elementCode)
    ResetProcessed->>Element: Reset processed state and restore textContent
    ResetProcessed->>Init: Resolve resetProcessed promise
    Init->>LoadMermaid: Call loadMermaid('dark')
    LoadMermaid->>Mermaid: Initialize and run with 'dark' theme

    Note over Init: Event Listener for 'light-theme-set'
    Document->>ResetProcessed: Trigger resetProcessed() on light-theme-set
    ResetProcessed->>Document: querySelectorAll(elementCode)
    ResetProcessed->>Element: Reset processed state and restore textContent
    ResetProcessed->>Init: Resolve resetProcessed promise
    Init->>LoadMermaid: Call loadMermaid('default')
    LoadMermaid->>Mermaid: Initialize and run with 'default' theme

    Note over Init: Check local storage theme
    Init->>LocalStorage: Retrieve localStorage.theme
    LocalStorage->>Init: Return 'dark' or other
    Init->>LoadMermaid: Call loadMermaid based on theme
    LoadMermaid->>Mermaid: Initialize and run with theme

```

## المشكلة

القضية هي أنك تحتاج إلى بدء حورية البحر لوضع الموضوع، ولا يمكنك تغييره بعد ذلك. على أي حال إذا كنت تريد إعادة توضيحه على رسم بياني مُنشأ بالفعل، فإنه لا يستطيع إعادة صياغة الرسم التخطيطي كما أن البيانات ليست مخزنة في DOM.

## الإحلال

لذا بعد الكثير من الحفر والمحاولة لمعرفة كيفية القيام بذلك، وجدت حلاً في [هذا مُنتج المُنتمِج](https://github.com/mermaid-js/mermaid/issues/1945)

ومع ذلك لا تزال لديها بعض المسائل، لذلك كان علي أن أغيرها قليلاً لأجعلها تعمل.

### 

هذا الموقع مبني على موضوع "تايلويند" الذي جاء مع محول موضوع فظيع جداً

سترى أن هذا يقوم بالعديد من الأشياء حول تبديل السمة، وضع السمة لما يتم تخزينه في التخزين المحلي، تغيير زوجين من مسودات الموضة لبسط mde و تسليط الضوء. js ومن ثم تطبيق السمة.

```javascript
export  function globalSetup() {
    const lightStylesheet = document.getElementById('light-mode');
    const darkStylesheet = document.getElementById('dark-mode');
    const simpleMdeDarkStylesheet = document.getElementById('simplemde-dark');
    const simpleMdeLightStylesheet = document.getElementById('simplemde-light');
    return {
        isMobileMenuOpen: false,
        isDarkMode: false,
        // Function to initialize the theme based on localStorage or system preference
        themeInit() {
            if (
                localStorage.theme === "dark" ||
                (!("theme" in localStorage) &&
                    window.matchMedia("(prefers-color-scheme: dark)").matches)
            ) {
                localStorage.theme = "dark";
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
              
                this.applyTheme(); // Apply dark theme stylesheets
            } else {
                localStorage.theme = "base";
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
                this.applyTheme(); // Apply light theme stylesheets
            }
        },

        // Function to switch the theme and update the stylesheets accordingly
        themeSwitch() {
            if (localStorage.theme === "dark") {
                localStorage.theme = "light";
                document.body.dispatchEvent(new CustomEvent('light-theme-set'));
                document.documentElement.classList.remove("dark");
                document.documentElement.classList.add("light");
                this.isDarkMode = false;
            } else {
                localStorage.theme = "dark";
                document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
                document.documentElement.classList.add("dark");
                document.documentElement.classList.remove("light");
                this.isDarkMode = true;
            }
            this.applyTheme(); // Apply the theme stylesheets after switching
        },

        // Function to apply the appropriate stylesheets based on isDarkMode
        applyTheme() {
         
            if (this.isDarkMode) {
                // Enable dark mode stylesheets
                lightStylesheet.disabled = true;
                darkStylesheet.disabled = false;
                simpleMdeLightStylesheet.disabled = true;
                simpleMdeDarkStylesheet.disabled = false;
            } else {
                // Enable light mode stylesheets
                lightStylesheet.disabled = false;
                darkStylesheet.disabled = true;
                simpleMdeLightStylesheet.disabled = false;
                simpleMdeDarkStylesheet.disabled = true;
            }
        }
    };
}
```

## إنشاء

وفيما يلي الإضافات الرئيسية لمحول الحوريات:

```javascript
  document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
    document.body.dispatchEvent(new CustomEvent('light-theme-set'));
```

هذان الحدثان يستخدمان في عنصر مقايضة السمة الخاص بنا لإعادة تنصيب رسومات حورية البحر.

### علىLoad / htmx: بعد Swap

(ب) في `main.js` أُعِدّ مُدَوِّل الموضوع. استوردت ايضاً `mdeswitch` الملف الذي يحتوي على رمز التبديل.

```javascript
import "./mdeswitch";
addEventListener("DOMContentLoaded", () => {
    window.initMermaid();
});
addEventListener('htmx:afterSwap', function(evt) {
    window.initMermaid();
});
```

## مُعمَّمْتich

هذا هو ملفّ يحتوي رمز لـ تغيير لـ حورية.
(الفظ [رسم الرسوم الرسوم](#the-diagram) عرض متتالي الأحداث التي تحدث عندما يُبدّل الموضوع)

```javascript
(function(window){
    'use strict'

    const elementCode = 'div.mermaid'
    const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
    const saveOriginalData = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    element.setAttribute('data-original-code',encodeURIComponent( element.textContent));
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }
    const resetProcessed = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    if(element.getAttribute('data-original-code') != null){
                        element.removeAttribute('data-processed')
                        element.textContent =decodeURIComponent( element.getAttribute('data-original-code'));
                    }
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }

    const init = ()=>{

        saveOriginalData()
            .catch( console.error )
        document.body.addEventListener('dark-theme-set', ()=>{
            resetProcessed()
                .then(() =>{
                    loadMermaid('dark');
                    console.log("dark theme set")})
                .catch(console.error)
        })
        document.body.addEventListener('light-theme-set', ()=>{
            resetProcessed()
                .then(() =>{
                    loadMermaid('default');
                    console.log("dark theme set")})
                .catch(console.error)
        })
        let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
        }
        else{
            loadMermaid('default')
        }

    }
    window.initMermaid = init
})(window);
```

من الأسفل إلى الأعلى هنا.

1. `init` الدالة هي الدالة الرئيسية التي تُدعى عند تحميل الصفحة.

أولاً يحفظ المحتوى الأصلي لمخططات حورية البحر، هذه كانت قضية في النسخة التي نسختها منها، استخدموا 'innerHTML' التي لم تعمل بالنسبة لي كبعض الرسوم البيانية تعتمد على خطوط جديدة التي تقوم بالتجزئة.

ومن ثم تضيف مستمعين لحدثين من أجل `dark-theme-set` وقد عقد مؤتمراً بشأن `light-theme-set` أحداث أحداث أحداث. وعندما تُطلق هذه الأحداث فإنها تعيد ضبط البيانات المجهزة ثم تعيد تأطير رسومات حورية البحر مع الموضوع الجديد.

ثم تقوم بعد ذلك بفحص التخزين المحلي للموضوع وتبدأ رسوم الحوريات مع الموضوع المناسب.

```javascript
let isDarkMode = localStorage.theme === 'dark';
        if(isDarkMode) {
            loadMermaid('dark');
         }
         else{
             loadMermaid('default')
         }
```

### 

مفتاح هذا الشيء كله هو تخزين ثم استعادة المحتوى الموجود في `<div class="mermaid"><div>` التي تحتوي على علامة حورية البحر من مواقعنا

سترى أن هذا فقط يرتب وعداً يقوم بالدورات من خلال كل العناصر ويخزن المحتوى الأصلي في `data-original-code` ........... ،..............................................................................................................

```javascript
    const saveOriginalData = function(){
        return new Promise((resolve, reject) => {
            try {
                var els = document.querySelectorAll(elementCode),
                    count = els.length;
                if(!els || count ===0 ) resolve ();
                els.forEach(element => {
                    element.setAttribute('data-original-code',encodeURIComponent(element.textContent))
                    count--
                    if(count == 0){
                        resolve()
                    }
                });
            } catch (error) {
                reject(error)
            }
        })
    }
```

`resetProcessed` هو نفسه باستثناء العكس العكسي حيث يأخذ الرفعة من `data-original-code` اضف وارجعها الى العنصر
ملاحظة أيضاً `encodeURIComponent` القيمة كما أَفْهمُ بأنّ بَعْض الأوتارِ ما كَانتْ تُخزّنُ بشكل صحيح.

### بوصة بوصةitt

الآن لدينا كل هذه البيانات التي يمكننا إعادة تأريخها للحورية لتطبيق موضوعنا الجديد وإعادة إدخال مخطط SVG في مخرجاتنا HTML.

```javascript
 const loadMermaid = function(theme) {
        window.mermaid.initialize({theme})
        window.mermaid.run()
    }
```

## في الإستنتاج

كان هذا قليلا من الألم لمعرفة ذلك، ولكن أنا سعيد لأنني فعلت. آمل أن يساعد هذا شخصاً آخر هناك يحاول أن يفعل نفس الشيء